# CURRENT_STATE - Estado actual del codigo

> Reauditado 2026-07-26 contra HEAD. Sesion larga: revision de arquitectura + modulo Tenants/Store completo + infra Docker Compose.
> Refleja el codigo real, no la documentacion ideal. Ver [BACKLOG.md](BACKLOG.md) seccion "DEUDA TECNICA / ARQUITECTURA" para gaps de escalabilidad no listados en la tabla de abajo.
>
> **Smoke test real (2026-07-26)**: `docker compose up -d` + migraciones + API corriendo, flujo
> completo probado contra SQL Server y Keycloak reales (no fakes): onboarding → Keycloak crea el
> Owner → activacion SUPERADMIN → `GET/PUT /api/store` → crear un Product real → invitar un Staff
> (F4-05) → Carrito completo (F6, add/acumular/get/update/delete). Encontro y arreglo 2 bugs que los
> tests con fakes no podian atrapar (Tenants) — ver C-39/C-40 en BACKLOG.md; Carrito paso sin bugs.

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
| **Orders** | ? No iniciado | Fase 7 |
| **Payments** | ? No iniciado | Fase 8 |

---

## Deuda tecnica de arquitectura (ver detalle en BACKLOG.md)

Revision de escalabilidad/sostenibilidad/consistencia (2026-07-26). D-02 y D-04 se implementaron el mismo dia; D-01 se probo y se revirtio a proposito; D-03 queda pendiente:

- **D-01 Unit of Work — descartado a proposito**: se implemento `IUnitOfWork`/`EfUnitOfWork` y se revirtio en la misma sesion. `EShopyDbContext` ya ES un Unit of Work (trackea cambios, `SaveChangesAsync` los confirma atomicamente); agregar otra interfaz encima era abstraer una abstraccion sin un segundo repositorio que la necesite todavia. `EfProductRepository` vuelve a llamar `SaveChangesAsync` directamente. Revisar cuando una operacion necesite escribir a traves de mas de un repositorio en una sola transaccion (candidato: Checkout, F7-02).
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

## Tests

| Suite | Tests | Estado |
|---|---|---|
| `EShopy.Tests.Unit` | 85 tests | ? (incluye `CartTests`, `CartValidatorTests`, `TenantTests`, `SubscriptionTests`, `TenantValidatorTests`, `InviteTenantUserCommandValidatorTests`, `SubdomainResolverTests`) |
| `EShopy.Tests.Integration` | 13 tests | ? Incluye seguridad 401/403/200, onboarding, invitacion de usuarios y flujo de carrito end-to-end |

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
