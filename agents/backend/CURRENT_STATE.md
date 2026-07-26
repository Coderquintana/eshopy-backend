# CURRENT_STATE - Estado actual del codigo

> Reauditado 2026-07-26 contra HEAD. Sesion larga: revision de arquitectura + modulo Tenants/Store completo + infra Docker Compose + Carrito + Pedidos/Pagos (minimo).
> Refleja el codigo real, no la documentacion ideal. Ver [BACKLOG.md](BACKLOG.md) seccion "DEUDA TECNICA / ARQUITECTURA" para gaps de escalabilidad no listados en la tabla de abajo.
>
> **Smoke test real (2026-07-26)**: `docker compose up -d` + migraciones + API corriendo, flujo
> completo probado contra SQL Server y Keycloak reales (no fakes): onboarding → Keycloak crea el
> Owner → activacion SUPERADMIN → `GET/PUT /api/store` → crear un Product real → invitar un Staff
> (F4-05) → Carrito completo (F6, add/acumular/get/update/delete) → Checkout completo (F7,
> carrito → Order → Payment → admin list/detail/status) → **25 checkouts concurrentes reales** contra
> el mismo tenant/producto. Encontro y arreglo 3 bugs que los tests con fakes no podian atrapar
> (Tenants: C-39/C-40; Orders: C-45, violacion de indice unico bajo contencion real en vez de
> `DbUpdateConcurrencyException`) — Carrito paso sin bugs.

---

## Estado por modulo

| Modulo | Estado | Notas |
|---|---|---|
| **Core / Infraestructura base** | ? Implementado | Middleware, BaseApiController, ErrorResponse, Result<T>, Global Query Filter, mapeo de `DbUpdateConcurrencyException`/violacion de indice unico a 409. Sin capa de Unit of Work generica a proposito (ver nota D-01 en BACKLOG.md); dos writers angostos (`ITenantOnboardingWriter`/`ITenantActivationWriter`) para los flujos que si escriben varios agregados en una transaccion |
| **Auth (Keycloak/JWT)** | ? Completo (Fase 2) | OIDC + RBAC por claim `permissions` + CORS por ambiente + headers de seguridad + UserContextAccessor |
| **Products (Catalog)** | ? Completo (MVP) | CQRS + Result<T> + SQL pagination + StoreId + transiciones validadas. `ProductService` ya no existe (reemplazado por Commands/Queries). FK reales a Tenants/Stores. RowVersion configurado pero no cableado end-to-end (ver D-03) |
| **Store** | ? Implementado | `EfStoreService`/`IStoreRepository` reales (reemplazan `InMemoryStoreService`). `GET/PUT /api/store` funcionando. `CurrencyCode` inmutable tras creacion |
| **Tenants** | ? Implementado (Fase 4) | `Tenant`/`TenantUser` reales, maquina de estados completa. `EfTenantResolver` reemplaza el diccionario en memoria (cache ~60s por subdominio). Onboarding (`POST /api/onboarding/tenants`) crea Tenant+Store+Owner(Keycloak)+Subscription atomicamente. Activacion manual SUPERADMIN implementada; webhook de pago sigue en Fase 8. Invitar Admin/Staff (`GET/POST /api/admin/users`) implementado y verificado en vivo (F4-05) |
| **Subscriptions** | ?? Minimo (Fase 4) | Entidad y maquina de estados completas, se crea en el onboarding. Sin integracion de pago real: `PriceAmount` siempre 0 (precios TBD), sin renovacion automatica ni webhook — todo eso es Fase 8 |
| **Carts** | ? Implementado (Fase 6) | `Cart`/`CartItem` — primer agregado con coleccion hija encapsulada (`Items` via backing field). `GET/POST/PUT/DELETE /api/cart[/items/{productId}]`, anonimo. Sin precio en `CartItem` (se lee en vivo). Falta solo F6-04 (limpieza de carritos expirados, no bloqueante) |
| **Orders** | ? Implementado (Fase 7) | `Order`/`OrderItem`, `ICheckoutWriter` (writer angosto, sin SQL crudo). `POST /api/checkout` + `GET /api/orders[/{id}]` + `PATCH /api/orders/{id}/status`. Verificado en vivo, incluye test de concurrencia real (25 checkouts simultaneos) que encontro y corrigio un bug (C-45) |
| **Payments** | ?? Minimo (prerequisito de Fase 7) | Solo lo que Checkout necesita: `Payment` entidad + `IPaymentProviderAdapter.InitiateAsync` + `FakePaymentProviderAdapter` (dev-only, siempre exitoso). Sin webhook, sin `PaymentEventsProcessed`, sin adapters reales Bancard/PagoPar — eso sigue en Fase 8 |

---

## Deuda tecnica de arquitectura (ver detalle en BACKLOG.md)

Revision de escalabilidad/sostenibilidad/consistencia (2026-07-26). D-02 y D-04 se implementaron el mismo dia; D-01 se probo y se revirtio a proposito; D-03 queda pendiente:

- **D-01 Unit of Work — descartado a proposito, confirmado por Checkout**: se implemento `IUnitOfWork`/`EfUnitOfWork` y se revirtio en la misma sesion. `EShopyDbContext` ya ES un Unit of Work (trackea cambios, `SaveChangesAsync` los confirma atomicamente); agregar otra interfaz encima era abstraer una abstraccion sin un segundo repositorio que la necesite todavia. `EfProductRepository` vuelve a llamar `SaveChangesAsync` directamente. Checkout (el candidato mencionado en la nota original) confirmo la decision: `EfCheckoutWriter` es un writer angosto igual que los de Tenants, no un `IUnitOfWork` generico — sigue sin haber necesidad real de uno.
- **D-02 Errores de concurrencia — resuelto**: `GlobalExceptionMiddleware` mapea `DbUpdateConcurrencyException` y `DbUpdateException` con `SqlException` 2601/2627 (violacion de indice unico) a 409 Conflict (`ErrorCodes.ConcurrencyConflict` / `ErrorCodes.Conflict`).
- **D-03 RowVersion decorativo — pendiente**: configurado como concurrency token en EF, pero los comandos no reciben la version del cliente, asi que no previene lost updates en la practica.
- **D-04 Resolucion de tenant sin tests — resuelto**: logica extraida a `EShopy.Application/Common/Tenants/SubdomainResolver.cs` (puro, sin dependencia de ASP.NET), con 9 tests en `EShopy.Tests.Unit/Tenants/SubdomainResolverTests.cs`.

Lo demas (capas, CQRS, Result<T>, validacion, indices/constraints en DB, multi-tenancy via Global Query Filter, RBAC) esta consistente y es una base solida para escalar.

---

## Seguridad (Fase 2)

Estado: **? Completo**

- Authentication: `AddAuthentication().AddJwtBearer(...)` con configuracion `Keycloak:*`.
- Authorization: policies completas por claim `permissions`:
  - `TenantsWrite`, `TenantsRead`
  - `StoreWrite`, `StoreRead`
  - `CatalogWrite`, `CatalogRead`
  - `OrdersWrite`, `OrdersRead`
  - `PaymentsRead`, `UsersManage`, `BillingManage`
- CORS por ambiente:
  - Dev: `http://localhost:4200`, `http://localhost:4201`
  - Prod: `https://*.eshopy.com.py`
- Headers de seguridad: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Strict-Transport-Security`.
- User context:
  - `EShopy.Application/Common/Identity/UserContext.cs`
  - `EShopy.Infrastructure/Identity/UserContextAccessor.cs`

---

## Products (modulo de referencia)

- Domain: `EShopy.Domain/Products/Product.cs`, `ProductStatus`, `ProductData`.
- Application: comandos/queries separados, handlers con `Result<T>`.
- API:
  - `EShopy.Api/Controllers/Admin/ProductsController.cs` (policies `CatalogRead/CatalogWrite`)
  - `EShopy.Api/Controllers/Public/ProductsController.cs` (`AllowAnonymous`)
- Infraestructura: `EShopy.Infrastructure/Products/EfProductRepository.cs`.

---

## Tenants / Store / Subscriptions

- Domain: `EShopy.Domain/Tenants/{Tenant,Store,TenantUser}.cs` (Tenant es global, sin TenantId),
  `EShopy.Domain/Subscriptions/Subscription.cs`.
- Application: `EShopy.Application/Tenants/` (repos, `ITenantOnboardingWriter`/`ITenantActivationWriter`,
  `CreateTenantCommand`, `ActivateTenantCommand`, `UpdateStoreCommand`, `GetStoreQuery`, `GetTenantByIdQuery`),
  `EShopy.Application/Subscriptions/ISubscriptionRepository.cs`,
  `EShopy.Application/Common/Identity/IKeycloakUserProvisioner.cs`.
- API:
  - `EShopy.Api/Controllers/Public/OnboardingController.cs` (`AllowAnonymous`)
  - `EShopy.Api/Controllers/Admin/TenantsController.cs` (`TenantsRead`/`TenantsWrite`, SUPERADMIN)
  - `EShopy.Api/Controllers/Public/StoreController.cs` (GET anonimo, PUT `StoreWrite`)
- Infraestructura:
  - `EShopy.Infrastructure/Tenants/` — `EfTenantRepository`, `EfStoreRepository`, `EfTenantResolver`
    (cache `IMemoryCache` ~60s), `EfTenantOnboardingWriter`, `EfTenantActivationWriter`
  - `EShopy.Infrastructure/Subscriptions/EfSubscriptionRepository.cs`
  - `EShopy.Infrastructure/Identity/KeycloakAdminClient.cs` — Keycloak Admin REST API real
    (client-credentials grant, service account de `eshopy-api`)

---

## Carts

- Domain: `EShopy.Domain/Carts/{Cart,CartItem}.cs` — `Cart` es agregado raiz, `Items` es
  `IReadOnlyList<CartItem>` respaldado por un campo privado (`_items`), toda mutacion pasa por
  metodos de `Cart` (`AddItem`/`UpdateItemQuantity`/`RemoveItem`).
- Application: `EShopy.Application/Carts/` (`ICartRepository`, Commands `Add/UpdateQuantity/Remove`,
  Query `GetCart`, `CartMappings` — arma el DTO uniendo `Cart` con `Product` en vivo, sin snapshot de precio).
- API: `EShopy.Api/Controllers/Public/CartController.cs` (`AllowAnonymous`, header `X-Cart-Token`).
- Infraestructura: `EShopy.Infrastructure/Carts/EfCartRepository.cs`,
  `Persistence/Configurations/{Cart,CartItem}Configuration.cs` (primera coleccion encapsulada del
  proyecto — `Navigation(...).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field)`).
  `IProductRepository.GetByIdsAsync` (batch, nuevo) evita N+1 al armar el DTO del carrito.

---

## Orders / Payments (minimo)

- Domain: `EShopy.Domain/Orders/{Order,OrderItem,OrderStatus,OrderItemData}.cs` — `Order` es agregado
  raiz con coleccion encapsulada (`Items`, mismo patron que `Cart`). `OrderNumber` empieza en 0,
  asignado por `ICheckoutWriter` via `AssignOrderNumber` (idempotente a proposito, ver C-45).
  `EShopy.Domain/Payments/{Payment,PaymentStatus}.cs`. `EShopy.Domain/Common/Counters/TenantCounter.cs`
  (contador atomico generico por tenant, `CurrentValue` concurrency token EF).
- Application: `EShopy.Application/Orders/` (`IOrderRepository`, `ICheckoutWriter`, Commands
  `Checkout`/`ChangeOrderStatus`, Queries `GetOrderById`/`GetOrders`),
  `EShopy.Application/Common/Payments/IPaymentProviderAdapter.cs` (solo `Provider` + `InitiateAsync` —
  metodos de webhook deliberadamente no incluidos todavia, ver `domain/payments.md`).
- API:
  - `EShopy.Api/Controllers/Public/CheckoutController.cs` (`AllowAnonymous`, header `X-Cart-Token`)
  - `EShopy.Api/Controllers/Admin/OrdersController.cs` (`OrdersRead`/`OrdersWrite`)
- Infraestructura: `EShopy.Infrastructure/Orders/{EfOrderRepository,EfCheckoutWriter}.cs`,
  `EShopy.Infrastructure/Payments/FakePaymentProviderAdapter.cs` (dev-only, siempre exitoso),
  `Persistence/Configurations/{Order,OrderItem,Payment,TenantCounter}Configuration.cs`. FK circular
  Order↔Payment: solo `Payments.OrderId` tiene FK real, `Orders.PaymentId` es una columna sin
  constraint (evita el ciclo, ver comentarios en `OrderConfiguration.cs`).
- `EfCheckoutWriter.CreateAsync` reintenta hasta 5 veces atrapando tanto `DbUpdateConcurrencyException`
  como `DbUpdateException` con `SqlException` 2601/2627 — ver C-45 para el bug real que hizo falta el
  segundo catch.

---

## Tests

| Suite | Tests | Estado |
|---|---|---|
| `EShopy.Tests.Unit` | 115 tests | ? (incluye `CartTests`, `CartValidatorTests`, `TenantTests`, `SubscriptionTests`, `TenantValidatorTests`, `InviteTenantUserCommandValidatorTests`, `SubdomainResolverTests`, `OrderTests`, `PaymentTests`, `CheckoutCommandValidatorTests`) |
| `EShopy.Tests.Integration` | 17 tests | ? Incluye seguridad 401/403/200, onboarding, invitacion de usuarios, flujo de carrito y flujo de checkout end-to-end (`CheckoutFlowTests`: checkout completo, email invalido, carrito vacio, transicion de estado invalida) |

Nuevos tests de seguridad:

- `EShopy.Tests.Integration/Security/AuthorizationTests.cs`
  - `GetProducts_WithoutToken_Returns401`
  - `GetProducts_WithCatalogReadPermission_Returns200`
  - `CreateProduct_WithoutCatalogWritePermission_Returns403`

Flujo de onboarding:

- `EShopy.Tests.Integration/Smoke/OnboardingFlowTests.cs`
  - `OnboardingFlow_ShouldCreateAndActivateTenant` (create → 201 PendingPayment → activate SUPERADMIN → 200 Active)
  - `CreateTenant_WithDuplicateSubdomain_ShouldReturn409`

Soporte de tests:

- `EShopy.Tests.Integration/Support/SecurityWebApplicationFactory.cs`
- `EShopy.Tests.Integration/Support/TestJwtTokenFactory.cs`
- `EShopy.Tests.Integration/Support/InMemoryProductRepository.cs`
- `EShopy.Tests.Integration/Support/InMemoryTenantsState.cs` + fakes de Tenants/Store/Subscriptions/Keycloak
  (mismo patron que `InMemoryProductRepository`, sin DB/Keycloak real)
- `EShopy.Tests.Integration/Support/InMemoryCartRepository.cs`
- `EShopy.Tests.Integration/Support/{InMemoryOrdersState,InMemoryOrderRepository,InMemoryCheckoutWriter}.cs`
  (mismo patron: `ICheckoutWriter` in-memory simula la asignacion atomica con un lock, no el
  concurrency-token EF real — esa garantia se prueba contra SQL Server real, ver C-45)

---

## Notas operativas

- DB `EShopy.Dev` en el contenedor SQL Server del `docker-compose.yml` (`localhost:1433`). Ver
  `docs/keycloak-setup.md` §0 para levantar todo el entorno local.
- Migraciones aplicadas:
  - `20260207035710_InitialCreate`
  - `20260207042545_AddAppEntityBaseToProducts`
  - `20260221000601_AddStoreIdToProducts`
  - `20260726164030_AddTenantsStoresSubscriptions`
  - `20260726183727_AddCartsCartItems`
  - `20260726191023_AddOrdersPaymentsTenantCounters`
- Si se elimina manualmente una tabla, EF no la recrea al iniciar mientras `__EFMigrationsHistory` siga marcado; ejecutar `dotnet ef database update` con historial consistente. (B-02 sigue abierto: no hay auto-migracion controlada en el arranque)

---

## Configuracion de desarrollo

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=EShopy.Dev;User Id=sa;Password=EShopy_Dev_2026!;TrustServerCertificate=True;"
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/eshopy",
    "Audience": "eshopy-api",
    "RequireHttpsMetadata": false,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "AdminBaseUrl": "http://localhost:8080",
    "AdminClientId": "eshopy-api",
    "AdminClientSecret": "eshopy-api-secret"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "http://localhost:4201"
    ]
  }
}
```

- DB + Keycloak: `docker compose up -d` (ver `docker-compose.yml` en la raiz)
- Keycloak: `localhost:8080`, realm `eshopy` (import automatico), cliente `eshopy-api` (tambien usado
  para la Admin API — service account con roles `realm-management`/`manage-users`,`view-users`)
