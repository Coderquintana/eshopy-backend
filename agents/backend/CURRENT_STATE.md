# CURRENT_STATE - Estado actual del codigo

> Reauditado 2026-07-26 contra HEAD (d531917). Sin commits nuevos desde 2026-02-20 (pausa de ~5 meses).
> Refleja el codigo real, no la documentacion ideal. Ver [BACKLOG.md](BACKLOG.md) seccion "DEUDA TECNICA / ARQUITECTURA" para gaps de escalabilidad no listados en la tabla de abajo.

---

## Estado por modulo

| Modulo | Estado | Notas |
|---|---|---|
| **Core / Infraestructura base** | ? Implementado | Middleware, BaseApiController, ErrorResponse, Result<T>, Global Query Filter, mapeo de `DbUpdateConcurrencyException`/violacion de indice unico a 409. Sin capa de Unit of Work explicita a proposito (ver nota D-01 en BACKLOG.md) |
| **Auth (Keycloak/JWT)** | ? Completo (Fase 2) | OIDC + RBAC por claim `permissions` + CORS por ambiente + headers de seguridad + UserContextAccessor |
| **Products (Catalog)** | ? Completo (MVP) | CQRS + Result<T> + SQL pagination + StoreId + transiciones validadas. `ProductService` ya no existe (reemplazado por Commands/Queries). RowVersion configurado pero no cableado end-to-end (ver D-03) |
| **Store (abstraccion)** | ?? Placeholder | IStoreService + InMemoryStoreService retorna PYG fijo |
| **Tenants** | ? Pendiente | Solo InMemoryTenantResolver placeholder (2 tenants hardcodeados). Resolucion de subdominio ahora en `SubdomainResolver` (puro, testeado) |
| **Carts** | ? No iniciado | Fase 6 |
| **Orders** | ? No iniciado | Fase 7 |
| **Payments** | ? No iniciado | Fase 8 |
| **Subscriptions** | ? No iniciado | Fase 4 |

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

## Tests

| Suite | Tests | Estado |
|---|---|---|
| `EShopy.Tests.Unit` | 33 tests | ? (incluye 9 nuevos de `SubdomainResolverTests`) |
| `EShopy.Tests.Integration` | 5 tests | ? Incluye seguridad 401/403/200 |

Nuevos tests de seguridad:

- `EShopy.Tests.Integration/Security/AuthorizationTests.cs`
  - `GetProducts_WithoutToken_Returns401`
  - `GetProducts_WithCatalogReadPermission_Returns200`
  - `CreateProduct_WithoutCatalogWritePermission_Returns403`

Soporte de tests:

- `EShopy.Tests.Integration/Support/SecurityWebApplicationFactory.cs`
- `EShopy.Tests.Integration/Support/TestJwtTokenFactory.cs`
- `EShopy.Tests.Integration/Support/InMemoryProductRepository.cs`

---

## Notas operativas

- DB `EShopy.Dev` validada en `localhost\\SQLEXPRESS`.
- Migraciones aplicadas:
  - `20260207035710_InitialCreate`
  - `20260207042545_AddAppEntityBaseToProducts`
  - `20260221000601_AddStoreIdToProducts`
- Si se elimina manualmente `Products`, EF no la recrea al iniciar mientras `__EFMigrationsHistory` siga marcado; ejecutar `dotnet ef database update` con historial consistente.

---

## Configuracion de desarrollo

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=EShopy.Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/eshopy",
    "Audience": "eshopy-api",
    "RequireHttpsMetadata": false,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "http://localhost:4201"
    ]
  }
}
```

- DB: SQL Server Express local, base `EShopy.Dev`
- Keycloak: `localhost:8080`, realm `eshopy`, cliente recomendado para admin/API `eshopy-api`
