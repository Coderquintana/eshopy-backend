# CURRENT_STATE — Estado actual del código

> Snapshot al 2026-02-19. Refleja el código real, no la documentación ideal.

---

## Estado por módulo

| Módulo | Estado | Notas |
|---|---|---|
| **Core / Infraestructura base** | ✅ Implementado | Middleware, BaseController, ErrorResponse, Result<T> |
| **Auth (Keycloak/JWT)** | ✅ Configurado | JWT Bearer + policies en Program.cs. Keycloak no testeado contra backend real |
| **Products (Catalog)** | ⚠️ Parcial | Domain + API + Repository OK. Ver deuda técnica abajo |
| **Tenants** | ❌ Pendiente | Solo InMemoryTenantResolver placeholder |
| **Store** | ❌ Skeleton | StoreController devuelve datos hardcodeados |
| **Carts** | ❌ No iniciado | Fase 6 |
| **Orders** | ❌ No iniciado | Fase 7 |
| **Payments** | ❌ No iniciado | Fase 8 |
| **Subscriptions** | ❌ No iniciado | Fase 4 |

---

## Implementación real del módulo Products

### Lo que está implementado y funciona

```
EShopy.Domain/Products/
  Product.cs              ← Aggregate root. Create(), UpdateDetails(), ChangeStatus()
  ProductData.cs          ← record para columna Data JSON
  ProductStatus.cs        ← enum: Draft(0), Active(1), Archived(2)

EShopy.Application/Products/
  IProductRepository.cs   ← contrato repo
  IProductService.cs      ← contrato servicio
  ProductService.cs       ← implementación (PENDIENTE refactor a Result<T>)
  Contracts/
    ProductAdminDto.cs    ← DTO admin (Id, Slug, Sku, Name, Price, Stock, Status, fechas)
    ProductPublicDto.cs   ← DTO público (Id, Slug, Name, Description, Price, Currency)
  Requests/
    CreateProductRequest.cs
    UpdateProductRequest.cs
    ChangeProductStatusRequest.cs

EShopy.Infrastructure/Products/
  EfProductRepository.cs  ← EF Core real (IMPLEMENTADO)
  InMemoryProductRepository.cs ← fallback (aún existe, no en DI activa)

EShopy.Infrastructure/Persistence/
  EShopyDbContext.cs      ← DbContext con DbSet<Product>
  Configurations/
    ProductConfiguration.cs ← EF mapping con índices y HasComment

EShopy.Api/Controllers/
  Admin/ProductsController.cs   ← [Authorize(Policy="CatalogWrite")] en cada action
  Public/ProductsController.cs  ← [AllowAnonymous]
```

### Lo que NO está implementado (deuda técnica activa)

| Item | Archivo(s) afectado(s) | Consecuencia actual |
|---|---|---|
| **Result<T> en Application** | `ProductService.cs` | Lanza excepciones; `GlobalExceptionMiddleware` las captura, pero rompe el patrón |
| **StoreId en Product** | `Product.cs`, `ProductConfiguration.cs`, migración | FK obligatoria según GOVERNANCE aún ausente |
| **Paginación en SQL** | `EfProductRepository.cs` líneas ~30-40 | Carga todos los registros en memoria, luego pagina en servicio |
| **CurrencyCode hardcodeado** | `ProductService.cs` | "PYG" literal; debe venir del Store |
| **ProductService monolítico** | `ProductService.cs` | Commands y Queries mezclados; debe separarse en handlers |
| **InMemoryTenantResolver** | `Infrastructure/Tenants/` | Siempre retorna 'localhost' como subdominio — no configurable |
| **Global Query Filter** | `EShopyDbContext.cs` | Sin filtro automático de TenantId — aislamiento manual en cada query |
| **Interceptor TenantId** | No existe aún | No impide SaveChanges sin TenantId |
| **StoreController datos reales** | `StoreController.cs` | Retorna valores hardcodeados (Guid literal, "PYG", etc.) |

---

## Archivos clave y su estado

| Archivo | Estado | Notas |
|---|---|---|
| `EShopy.Api/Program.cs` | ✅ OK | Pipeline correcto, DI configurada |
| `EShopy.Api/Controllers/Admin/ProductsController.cs` | ✅ OK | [Authorize] en todas las actions |
| `EShopy.Api/Controllers/Public/ProductsController.cs` | ✅ OK | [AllowAnonymous] correcto |
| `EShopy.Api/Controllers/Public/StoreController.cs` | ⚠️ Skeleton | Datos hardcodeados |
| `EShopy.Domain/Products/Product.cs` | ⚠️ Incompleto | Falta `StoreId` |
| `EShopy.Application/Products/ProductService.cs` | ⚠️ Refactor pendiente | Sin Result<T>, CurrencyCode hardcoded |
| `EShopy.Infrastructure/Products/EfProductRepository.cs` | ⚠️ Parcial | Sin paginación SQL |
| `EShopy.Infrastructure/Persistence/EShopyDbContext.cs` | ⚠️ Incompleto | Sin Global Query Filter |

---

## Próximo paso recomendado

**Fase 5 — Refactor Catalog** (prerequisito: Fase 2 y 3 parciales):

1. Agregar `StoreId` a `Product.cs` + `ProductConfiguration.cs` + nueva migración EF
2. Refactorizar `ProductService` → Command/Query handlers con `Result<T>`
3. Implementar paginación SQL en `EfProductRepository`
4. Eliminar CurrencyCode hardcodeado (tomar del Store)
5. Agregar Global Query Filter en `EShopyDbContext`

Ver `BACKLOG.md` para el detalle completo de tareas por fase.

---

## Migraciones EF Core (estado)

| Migración | Fecha | Descripción |
|---|---|---|
| `20260207035710_InitialCreate` | 2026-02-07 | Schema inicial de Products |
| `20260207042545_AddAppEntityBaseToProducts` | 2026-02-07 | Columnas AppEntity (auditoría, RowVersion, Data) |

**Pendiente:** migración para `StoreId` en Products + tablas Tenants, Stores, Orders, Carts, Payments, Subscriptions.

---

## Configuración de desarrollo

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Default": "Server=localhost\\SQLEXPRESS;Database=EShopy.Dev;Trusted_Connection=True;TrustServerCertificate=True"
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
