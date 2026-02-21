# CURRENT_STATE — Estado actual del código

> Snapshot al 2026-02-20. Refleja el código real, no la documentación ideal.

---

## Estado por módulo

| Módulo | Estado | Notas |
|---|---|---|
| **Core / Infraestructura base** | ✅ Implementado | Middleware, BaseApiController, ErrorResponse, Result<T>, Global Query Filter |
| **Auth (Keycloak/JWT)** | ✅ Configurado | JWT Bearer + políticas CatalogRead/CatalogWrite/OrdersRead/OrdersWrite/UsersManage |
| **Products (Catalog)** | ✅ Completo (MVP) | CQRS + Result<T> + SQL pagination + StoreId + transiciones validadas |
| **Store (abstracción)** | ⚠️ Placeholder | IStoreService + InMemoryStoreService retorna PYG fijo |
| **Tenants** | ❌ Pendiente | Solo InMemoryTenantResolver placeholder |
| **Carts** | ❌ No iniciado | Fase 6 |
| **Orders** | ❌ No iniciado | Fase 7 |
| **Payments** | ❌ No iniciado | Fase 8 |
| **Subscriptions** | ❌ No iniciado | Fase 4 |

---

## Módulo Products — estructura CQRS implementada

```
EShopy.Domain/Products/
  Product.cs              ← StoreId ✅, ChangeStatus con validación de transiciones ✅
  ProductData.cs          ← record para columna Data JSON
  ProductStatus.cs        ← enum : byte { Draft=0, Active=1, Archived=2 }

EShopy.Domain/Common/
  Results/Result.cs       ← Result<T> y Result sin genérico
  Errors/ErrorCodes.cs    ← incluye PRODUCT_NOT_AVAILABLE, PRODUCT_INVALID_STATE

EShopy.Application/Products/
  IProductRepository.cs   ← GetAdminPagedAsync / GetPublicPagedAsync con PagedQuery
  ProductMappings.cs      ← ToAdminDto() / ToPublicDto() compartido
  Commands/
    CreateProductCommand.cs + Handler + Validator    ← retorna Result<ProductAdminDto>
    UpdateProductCommand.cs + Handler + Validator
    ChangeProductStatusCommand.cs + Handler + Validator
  Queries/
    GetProductsQuery.cs + Handler                    ← retorna Result<PagedResult<ProductAdminDto>>
    GetProductByIdQuery.cs + Handler
    GetPublicProductsQuery.cs + Handler              ← retorna Result<PagedResult<ProductPublicDto>>
    GetProductBySlugQuery.cs + Handler               ← solo productos Active
  Contracts/
    ProductAdminDto.cs
    ProductPublicDto.cs
    PagedQuery.cs          ← record PagedQuery(int Page=1, int PageSize=20)

EShopy.Application/Common/
  Stores/IStoreService.cs ← GetDefaultStoreAsync(tenantId) → StoreDto?
  Stores/StoreDto.cs      ← record StoreDto(Guid Id, string CurrencyCode)
  Context/TenantContext.cs
  Context/UserContext.cs

EShopy.Infrastructure/Products/
  EfProductRepository.cs  ← SQL pagination con SKIP/TAKE + LongCountAsync

EShopy.Infrastructure/Stores/
  InMemoryStoreService.cs ← placeholder: StoreId=11111111..., CurrencyCode="PYG"

EShopy.Infrastructure/Persistence/
  EShopyDbContext.cs       ← Global Query Filter por TenantId ✅
  EShopyDbContextFactory.cs ← IDesignTimeDbContextFactory para migrations
  Configurations/
    ProductConfiguration.cs ← StoreId ✅, Description HasMaxLength(5000) ✅

EShopy.Api/Controllers/
  BaseApiController.cs         ← FromResult<T>() mapea Result→ActionResult ✅
  Admin/ProductsController.cs  ← thin CQRS, GET=[CatalogRead], POST/PUT/PATCH=[CatalogWrite]
  Public/ProductsController.cs ← thin CQRS, [AllowAnonymous]

EShopy.Api/Middlewares/
  TenantResolutionMiddleware.cs ← excluye /health, /swagger, /api/onboarding/tenants, /api/payments/webhooks
  GlobalExceptionMiddleware.cs  ← incluye PRODUCT_INVALID_STATE→409, catch-all→500 ✅
```

---

## Tests — estado actual

| Suite | Tests | Estado |
|---|---|---|
| `EShopy.Tests.Unit` | 24 tests | ✅ Todos pasan |
| `EShopy.Tests.Integration` | 1 test smoke | ⚠️ Requiere DB + auth real |

### Tests unitarios de Products
- `Product_Create_WithValidData_ShouldReturnDraftProduct`
- `Product_Create_WithNegativePrice_ShouldThrowDomainException`
- `Product_Create_WithNegativeStock_ShouldThrowDomainException`
- `Product_Create_WithZeroPrice_ShouldSucceed`
- `Product_Create_SlugShouldBeNormalized`
- `Product_ChangeStatus_DraftToActive_ShouldSucceed`
- `Product_ChangeStatus_ActiveToArchived_ShouldSucceed`
- `Product_ChangeStatus_ArchivedToActive_ShouldSucceed`
- `Product_ChangeStatus_DraftToArchived_ShouldThrowDomainException`
- `Product_ChangeStatus_ActiveToDraft_ShouldThrowDomainException`
- `Product_ChangeStatus_ArchivedToDraft_ShouldThrowDomainException`
- `Product_ChangeStatus_SameStatus_ShouldBeIdempotent`
- Validators: 10 casos (slug, regex, maxLength, precio, stock, enum)

---

## Deuda técnica activa

| Item | Archivo(s) | Impacto |
|---|---|---|
| **InMemoryTenantResolver** | `Infrastructure/Tenants/` | Siempre retorna 'localhost' — no configurable |
| **InMemoryStoreService** | `Infrastructure/Stores/` | StoreId y CurrencyCode fijos; reemplazar con EfStoreService cuando exista tabla Stores |
| **StoreController skeleton** | `Api/Controllers/StoreController.cs` | Datos hardcodeados |
| **Sin tabla Stores** | `EShopyDbContext` | IStoreService no tiene backing real |
| **Sin interceptor TenantId** | No existe | No impide SaveChanges sin TenantId |
| **ProductImages** | No implementado | Pendiente módulo Images |

---

## Migraciones EF Core

| Migración | Fecha | Descripción |
|---|---|---|
| `20260207035710_InitialCreate` | 2026-02-07 | Schema inicial de Products |
| `20260207042545_AddAppEntityBaseToProducts` | 2026-02-07 | Columnas AppEntity (auditoría, RowVersion, Data) |
| `*_AddStoreIdToProducts` | 2026-02-20 | StoreId + Description HasMaxLength(5000) |

**Pendiente:** tablas Tenants, Stores, Orders, Carts, Payments, Subscriptions.

---

## Patrones de referencia (copiar para nuevos módulos)

El módulo Products es el módulo de referencia. Los demás módulos deben seguir su estructura:

1. **Domain**: entidad sellada heredando `AppEntity`, `enum Status : byte`, validaciones con `DomainException`
2. **Application**: CQRS con `Commands/` y `Queries/`, handlers retornan `Result<T>`, validators FluentValidation
3. **Infrastructure**: `EfXxxRepository` con SQL pagination, registrado en `DependencyInjection.cs`
4. **API**: controller thin, `FromResult()` en BaseApiController, `[Authorize(Policy=...)]` correctos

---

## Próximo paso recomendado

**Fase 6 — Módulo Tenants + Store real:**

1. Implementar tabla `Tenants` y `Stores` con migraciones
2. Reemplazar `InMemoryTenantResolver` con `EfTenantResolver`
3. Reemplazar `InMemoryStoreService` con `EfStoreService`
4. Implementar `TenantCounters` para OrderNumber atómico

Ver `BACKLOG.md` para el detalle completo.

---

## Configuración de desarrollo

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=lpc:localhost\\SQLEXPRESS;Database=EShopy.Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Auth": {
    "Authority": "http://localhost:8080/realms/eshopy",
    "Audience": "eshopy-api"
  }
}
```

- DB: SQL Server Express local, base `EShopy.Dev`
- Keycloak: `localhost:8080`, realm `eshopy`, cliente postman: `eshopy-postman` / `postman-secret`
- Si SQL Browser no activo: usar `lpc:` prefix en connection string
