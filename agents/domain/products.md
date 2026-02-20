# Domain — Products

> Entidad `Product`: aggregate root del módulo Catalog. Multi-tenant.

## Propiedades

| Propiedad | Tipo | Nullable | Regla |
|---|---|---|---|
| `Id` | `Guid` | No | PK, generado en `Create()` |
| `TenantId` | `Guid` | No | FK obligatorio, nunca del request |
| `Slug` | `string` | No | Lowercase, único por tenant. Normalizado en `Create()` |
| `Sku` | `string?` | Sí | Uppercase, máx 64 chars, único por tenant si presente |
| `Name` | `string` | No | Trim obligatorio |
| `Description` | `string?` | Sí | Trim o null si vacío |
| `Price` | `decimal` | No | `>= 0` |
| `CurrencyCode` | `string` | No | Tomado del Store; no en request. Uppercase, trim |
| `Status` | `ProductStatus` | No | Ver tabla de estados |
| `StockOnHand` | `int` | No | `>= 0`, obligatorio |
| `StoreId` | `Guid` | No | FK al Store del tenant (pendiente: no en código aún) |
| `Data` | `string?` | Sí | JSON serializado de `ProductData` |
| — columnas AppEntity — | | | Ver `architecture/database-schema.md` |

## Factory method

```csharp
// Archivo: EShopy.Domain/Products/Product.cs
Product.Create(
    tenantId: Guid,
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

## Reglas de dominio (invariantes)

- `Slug` único por `(TenantId, Slug)` — verificar en repo antes de crear/actualizar
- `Sku` único por `(TenantId, Sku)` WHERE Sku IS NOT NULL — verificar en repo
- `Price >= 0` — validación en `Create()` y `UpdateDetails()`
- `StockOnHand >= 0` — no puede ser negativo ni null
- `Name` no puede ser vacío ni solo espacios
- `CurrencyCode` no puede ser vacío — normalmente heredado del Store

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

// Cambiar estado (sin validar transición en dominio aún — validar en servicio)
product.ChangeStatus(status: ProductStatus, updatedAtUtc: DateTime)
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
| `ProductAdminDto` | Endpoints admin | Id, Slug, Sku, Name, Description, Price, CurrencyCode, Status, StockOnHand, CreatedAtUtc, UpdatedAtUtc |
| `ProductPublicDto` | Endpoints públicos | Id, Slug, Name, Description, Price, CurrencyCode |

## Requests

| Request | Campos | Validaciones |
|---|---|---|
| `CreateProductRequest` | Slug, Sku?, Name, Description?, Price, StockOnHand | Via FluentValidation |
| `UpdateProductRequest` | Name, Description?, Price, StockOnHand, Sku? | Via FluentValidation |
| `ChangeProductStatusRequest` | Status (enum) | Valor válido del enum |

> `CurrencyCode` NO va en requests — el backend lo toma del Store del tenant.

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
| [EShopy.Domain/Products/Product.cs](../../EShopy.Domain/Products/Product.cs) | Aggregate root |
| [EShopy.Domain/Products/ProductData.cs](../../EShopy.Domain/Products/ProductData.cs) | JSON data record |
| [EShopy.Application/Products/ProductService.cs](../../EShopy.Application/Products/ProductService.cs) | Servicio (pendiente refactor) |
| [EShopy.Infrastructure/Products/EfProductRepository.cs](../../EShopy.Infrastructure/Products/EfProductRepository.cs) | Repositorio EF |
| [EShopy.Api/Controllers/Admin/ProductsController.cs](../../EShopy.Api/Controllers/Admin/ProductsController.cs) | Controller admin |
| [EShopy.Api/Controllers/Public/ProductsController.cs](../../EShopy.Api/Controllers/Public/ProductsController.cs) | Controller público |
