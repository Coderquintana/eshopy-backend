# CURRENT_STATE - Estado actual del codigo

> Snapshot al 2026-02-21. Refleja el codigo real, no la documentacion ideal.

---

## Estado por modulo

| Modulo | Estado | Notas |
|---|---|---|
| **Core / Infraestructura base** | ? Implementado | Middleware, BaseApiController, ErrorResponse, Result<T>, Global Query Filter |
| **Auth (Keycloak/JWT)** | ? Completo (Fase 2) | OIDC + RBAC por claim `permissions` + CORS por ambiente + headers de seguridad + UserContextAccessor |
| **Products (Catalog)** | ? Completo (MVP) | CQRS + Result<T> + SQL pagination + StoreId + transiciones validadas |
| **Store (abstraccion)** | ?? Placeholder | IStoreService + InMemoryStoreService retorna PYG fijo |
| **Tenants** | ? Pendiente | Solo InMemoryTenantResolver placeholder |
| **Carts** | ? No iniciado | Fase 6 |
| **Orders** | ? No iniciado | Fase 7 |
| **Payments** | ? No iniciado | Fase 8 |
| **Subscriptions** | ? No iniciado | Fase 4 |

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
| `EShopy.Tests.Unit` | 24 tests | ? |
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
