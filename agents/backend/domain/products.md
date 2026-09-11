# Domain — Products

> Entidad `Product`: aggregate root del módulo Catalog. Multi-tenant.

## Propiedades

| Propiedad | Tipo | Nullable | Regla |
|---|---|---|---|
| `Id` | `Guid` | No | PK, generado en `Create()` |
| `TenantId` | `Guid` | No | FK obligatorio, nunca del request |
| `StoreId` | `Guid` | No | FK al Store del tenant. Resuelto por el handler vía `IStoreService`, nunca del request |
| `Slug` | `string` | No | Lowercase, único por tenant. Normalizado en `Create()` |
| `Sku` | `string?` | Sí | Uppercase, máx 64 chars, único por tenant si presente |
| `Name` | `string` | No | Trim obligatorio |
| `Description` | `string?` | Sí | Trim o null si vacío |
| `Price` | `decimal` | No | `>= 0` |
| `CurrencyCode` | `string` | No | Tomado del Store; no en request. Uppercase, trim |
| `Status` | `ProductStatus` | No | Ver tabla de estados |
| `StockOnHand` | `int` | No | `>= 0`, obligatorio |
| `Data` | `string?` | Sí | JSON serializado de `ProductData` |
| — columnas AppEntity (incluye `RowVersion`) — | | | Ver `architecture/database-schema.md` |

## Factory method

```csharp
// Archivo: EShopy.Domain/Products/Product.cs
Product.Create(
    tenantId: Guid,
    storeId: Guid,
    slug: string,           // se normaliza a lowercase
    sku: string?,           // se normaliza a uppercase o null
    name: string,
    description: string?,
    price: decimal,         // >= 0
    stockOnHand: int,       // >= 0
    currencyCode: string,   // tomado del Store
    createdAtUtc: DateTime
) → Product (Status = Draft)
```

## Estados y transiciones (ProductStatus)

| Estado | Valor | Visible en Storefront |
|---|---|---|
| `Draft` | 0 | No |
| `Active` | 1 | Sí |
| `Archived` | 2 | No |

| Desde → Hacia | Permitida |
|---|---|
| Draft → Active | ✅ |
| Draft → Archived | ❌ |
| Active → Archived | ✅ |
| Active → Draft | ❌ |
| Archived → Active | ✅ |
| Archived → Draft | ❌ |

La transición se valida **en el dominio**: `Product.ChangeStatus()` tira `DomainException(ErrorCodes.ProductInvalidState)`
si no es válida. No hay validación de transición en el handler.

## Concurrencia optimista

`Product` hereda `RowVersion` (rowversion SQL) de `AppEntity`. El flujo completo:

1. `ProductAdminDto.RowVersion` expone el rowversion actual codificado en Base64 (`ProductConcurrency.Encode`).
2. El admin manda ese mismo valor de vuelta en `UpdateProductCommand.RowVersion` / `ChangeProductStatusCommand.RowVersion`.
3. El handler lo decodifica (`ProductConcurrency.Decode`) y compara contra el `RowVersion` real del producto
   (`ProductConcurrency.Matches`, comparación en tiempo constante).
4. Si no coincide → `Result.Fail(ErrorCodes.ConcurrencyConflict)` → 409, sin tocar la entidad.
5. Si coincide → se aplica el cambio y `IProductRepository.UpdateAsync(product, expectedRowVersion, ct)` lo persiste
   (EF Core usa ese mismo array como filtro de concurrencia en el `UPDATE`).

Ver `EShopy.Application/Products/ProductConcurrency.cs`.

## Auditoría

`UpdateProductCommandHandler` y `ChangeProductStatusCommandHandler` llaman `IAuditLogger.LogAsync(...)` después de
persistir el cambio:

- **Cambio de precio**: solo si `previousPrice != product.Price`, evento `Product.ChangePrice`, detalle `"{antes} -> {despues}"`.
- **Cambio de estado**: siempre que la transición sea exitosa, evento `Product.ChangeStatus`, detalle `"{antes} -> {despues}"`.

Ver `EShopy.Tests.Integration/Smoke/ProductAuditFlowTests.cs` para los casos cubiertos.

## Reglas de dominio (invariantes)

- `Slug` único por `(TenantId, Slug)` — verificar en repo antes de crear/actualizar
- `Sku` único por `(TenantId, Sku)` WHERE Sku IS NOT NULL — verificar en repo
- `Price >= 0` — validación en `Create()` y `UpdateDetails()`
- `StockOnHand >= 0` — no puede ser negativo ni null
- `Name` no puede ser vacío ni solo espacios
- `CurrencyCode` no puede ser vacío — normalmente heredado del Store
- Transición de `Status` inválida → `DomainException(ProductInvalidState)` (ver tabla arriba)

## Métodos de actualización

```csharp
// Actualizar campos editables
product.UpdateDetails(
    name: string,
    description: string?,
    price: decimal,
    stockOnHand: int,
    sku: string?,
    updatedAtUtc: DateTime
)

// Cambiar estado (valida la transición, tira DomainException si no es válida)
product.ChangeStatus(newStatus: ProductStatus, updatedAtUtc: DateTime)
```

## ProductData (campo JSON extensible)

```csharp
// Archivo: EShopy.Domain/Products/ProductData.cs
public sealed record ProductData(
    string? AdditionalInfo,
    Dictionary<string, string>? Attributes,  // ej: {"color": "rojo", "talla": "M"}
    string? ExternalReference                 // ID en sistema externo
);

// Uso en Product:
product.SetData(new ProductData(...));
var data = product.DataJson; // ProductData?
```

## DTOs de transporte

| DTO | Usado por | Campos |
|---|---|---|
| `ProductAdminDto` | Endpoints admin | Id, Slug, Sku, Name, Description, Price, CurrencyCode, Status, StockOnHand, **RowVersion**, CreatedAtUtc, UpdatedAtUtc |
| `ProductPublicDto` | Endpoints públicos | Id, Slug, Name, Description, Price, CurrencyCode |

## Commands / Queries (CQRS liviano, un handler por caso de uso)

No existe un `ProductService` monolítico — cada caso de uso es un command/query + handler propio en
`EShopy.Application/Products/{Commands,Queries}/`.

| Command / Query | Campos | Notas |
|---|---|---|
| `CreateProductCommand` | Slug, Sku?, Name, Description?, Price, StockOnHand | `StoreId` y `CurrencyCode` los resuelve el handler vía `IStoreService`, no vienen del request |
| `UpdateProductCommand` | Id, Name, Description?, Price, StockOnHand, Sku?, **RowVersion** | `RowVersion` obligatorio para concurrencia |
| `ChangeProductStatusCommand` | Id, Status, **RowVersion** | Idem |
| `GetProductByIdQuery` / `GetProductBySlugQuery` | — | Admin y público respectivamente |
| `GetProductsQuery` (admin, `PagedQuery`) / `GetPublicProductsQuery` (solo `Active`) | — | Paginación resuelta en SQL vía `IProductRepository` |

> `CurrencyCode` NO va en ningún request — el backend lo toma del Store del tenant.

## Índices DB

| Índice | Tipo |
|---|---|
| `(TenantId, Slug)` | UNIQUE |
| `(TenantId, Sku)` WHERE Sku IS NOT NULL | UNIQUE filtrado |
| `(TenantId, Status)` | IX (búsqueda pública) |
| `(TenantId, Name)` | IX (búsqueda admin) |

## Archivos relevantes

| Archivo | Descripción |
|---|---|
| [EShopy.Domain/Products/Product.cs](../../../EShopy.Domain/Products/Product.cs) | Aggregate root |
| [EShopy.Domain/Products/ProductData.cs](../../../EShopy.Domain/Products/ProductData.cs) | JSON data record |
| [EShopy.Application/Products/Commands/](../../../EShopy.Application/Products/Commands/) | Create/Update/ChangeStatus + validators |
| [EShopy.Application/Products/Queries/](../../../EShopy.Application/Products/Queries/) | Get by id/slug, listados paginados |
| [EShopy.Application/Products/ProductConcurrency.cs](../../../EShopy.Application/Products/ProductConcurrency.cs) | Encode/decode/match de RowVersion |
| [EShopy.Infrastructure/Products/EfProductRepository.cs](../../../EShopy.Infrastructure/Products/EfProductRepository.cs) | Repositorio EF |
| [EShopy.Api/Controllers/Admin/ProductsController.cs](../../../EShopy.Api/Controllers/Admin/ProductsController.cs) | Controller admin |
| [EShopy.Api/Controllers/Public/ProductsController.cs](../../../EShopy.Api/Controllers/Public/ProductsController.cs) | Controller público |
