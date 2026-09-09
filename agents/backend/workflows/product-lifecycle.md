# Workflow — Product Lifecycle

> Flujo completo del ciclo de vida de un producto: Draft → Active → Archived.

## Concurrencia optimista

Todo `ProductAdminDto` incluye `rowVersion`, un string base64 que representa los 8 bytes de `rowversion` de SQL Server. Las mutaciones de producto existentes (`PUT` y `PATCH status`) deben reenviar el token recibido en la última lectura o respuesta.

- Falta `rowVersion`, no es base64 válido o no representa exactamente 8 bytes → `400 VALIDATION_ERROR`.
- El token ya no coincide con la fila actual → `409 CONCURRENCY_CONFLICT` sin aplicar la mutación.
- Si otra escritura ocurre después de la lectura del handler pero antes de `SaveChangesAsync`, `EfProductRepository` fija el token del cliente como `OriginalValue`; EF detecta la carrera y `GlobalExceptionMiddleware` devuelve el mismo `409 CONCURRENCY_CONFLICT`.
- Cada mutación exitosa devuelve un `ProductAdminDto` con un **nuevo** `rowVersion`; el cliente debe reemplazar el token anterior y nunca reintentar una escritura con el token viejo.

No hay migración nueva para D-03: `Products.RowVersion` ya existía y estaba configurado con `IsRowVersion()`.

## Diagrama de estados

```
         ┌──────────┐
  Create │          │ ChangeStatus(Active)
  ──────>│  Draft   │──────────────────────> ┌────────┐
         │          │                         │        │
         └──────────┘                  ┌────> │ Active │
                                       │      │        │
         ChangeStatus(Active)          │      └────────┘
         ──────────────────────────────┘           │
         (desde Archived)                          │ ChangeStatus(Archived)
                                                   │
                                                   v
                                            ┌──────────────┐
                                            │   Archived   │
                                            └──────────────┘
```

## Transiciones permitidas (tabla autoritativa)

| Desde | Hacia | Permitida | Acción en sistema |
|---|---|---|---|
| Draft | Active | ✅ | Producto visible en Storefront |
| Draft | Archived | ❌ | Retornar `PRODUCT_INVALID_STATE` (409) |
| Active | Archived | ✅ | Producto oculto del Storefront |
| Active | Draft | ❌ | Retornar `PRODUCT_INVALID_STATE` (409) |
| Archived | Active | ✅ | Producto visible en Storefront nuevamente |
| Archived | Draft | ❌ | Retornar `PRODUCT_INVALID_STATE` (409) |

## Flujo 1: Crear producto (Draft)

**Actor**: Tenant Admin/Owner
**Endpoint**: `POST /api/products`
**Auth**: `CatalogWrite`

```
1. Admin envía CreateProductRequest (slug, name, price, stockOnHand, sku?)
2. FluentValidation valida el request
3. ProductService verifica:
   a. TenantId resuelto de TenantContext
   b. Slug no existe para este tenant (IProductRepository.SlugExistsAsync)
   c. Sku no existe para este tenant si no es null (IProductRepository.SkuExistsAsync)
4. Product.Create(...) — crea con Status = Draft
5. IProductRepository.AddAsync(product)
6. Retorna ProductAdminDto con Status = "Draft" y `rowVersion` inicial
```

**Reglas de validación del request (FluentValidation):**
- `Slug`: requerido, máx 200 chars, solo letras, números y guiones
- `Name`: requerido, máx 300 chars
- `Price`: requerido, `>= 0`
- `StockOnHand`: requerido, `>= 0`
- `Sku`: opcional, máx 64 chars (normalizado a uppercase en dominio)
- `CurrencyCode`: **no va en el request**

## Flujo 2: Publicar producto (Draft → Active)

**Actor**: Tenant Admin/Owner
**Endpoint**: `PATCH /api/products/{id}/status`
**Auth**: `CatalogWrite`

```json
{
  "status": 1,
  "rowVersion": "<base64 recibido del ProductAdminDto>"
}
```

```
1. Admin envía status + rowVersion actual
2. ChangeProductStatusCommandHandler:
   a. GetByIdAsync — si no existe → NOT_FOUND (404)
   b. Compara rowVersion — si no coincide → CONCURRENCY_CONFLICT (409)
   c. Valida transición: Draft → Active ✅
   d. product.ChangeStatus(Active, utcNow)
   e. IProductRepository.UpdateAsync(product, expectedRowVersion)
   f. AuditLog `Product.ChangeStatus` con `Draft -> Active`
3. Retorna ProductAdminDto con Status = "Active" y un nuevo `rowVersion`
```

**Efecto en Storefront**: producto aparece en `GET /api/public/products`

## Flujo 3: Archivar producto (Active → Archived)

**Actor**: Tenant Admin/Owner
**Endpoint**: `PATCH /api/products/{id}/status`
**Auth**: `CatalogWrite`

```
1. Admin envía { "status": 2, "rowVersion": "<token actual>" } (Archived)
2. ChangeProductStatusCommandHandler:
   a. GetByIdAsync — si no existe → NOT_FOUND (404)
   b. Compara rowVersion — si no coincide → CONCURRENCY_CONFLICT (409)
   c. Valida transición: Active → Archived ✅
   d. product.ChangeStatus(Archived, utcNow)
   e. UpdateAsync(product, expectedRowVersion)
   f. AuditLog `Product.ChangeStatus` con `Active -> Archived`
3. Retorna ProductAdminDto con Status = "Archived" y un nuevo `rowVersion`
```

**Efecto en Storefront**: producto desaparece del catálogo público.
**Nota**: los OrderItems existentes conservan su snapshot — el archivado no afecta pedidos pasados.

## Flujo 4: Reactivar producto (Archived → Active)

Igual que Flujo 2 pero el producto parte desde `Archived`. Requiere el `rowVersion` actual y devuelve un token nuevo.

```
1. Admin envía { "status": 1, "rowVersion": "<token actual>" } (Active)
2. Valida rowVersion y transición: Archived → Active ✅
3. product.ChangeStatus(Active, utcNow) + UpdateAsync(product, expectedRowVersion)
4. AuditLog `Product.ChangeStatus` con `Archived -> Active`
```

## Flujo 5: Actualizar detalles de producto

**Actor**: Tenant Admin/Owner
**Endpoint**: `PUT /api/products/{id}`
**Auth**: `CatalogWrite`

```json
{
  "name": "Producto",
  "description": "Descripción",
  "price": 1500,
  "stockOnHand": 5,
  "sku": "SKU-001",
  "rowVersion": "<base64 recibido del ProductAdminDto>"
}
```

```
1. Admin envía UpdateProductRequest (name, description, price, stockOnHand, sku?, rowVersion)
2. UpdateProductCommandHandler:
   a. GetByIdAsync — si no existe → NOT_FOUND (404)
   b. Compara rowVersion — si no coincide → CONCURRENCY_CONFLICT (409)
   c. Si Sku cambia: verificar unicidad en tenant
   d. Guardar el precio anterior
   e. product.UpdateDetails(...)
   f. UpdateAsync(product, expectedRowVersion)
   g. Si el precio cambió: AuditLog `Product.ChangePrice` con `OldPrice -> NewPrice`
3. Retorna ProductAdminDto actualizado con un nuevo `rowVersion`
```

**Nota**: `Slug` no es editable una vez creado (URL permanente). Para cambiar slug, archivar y crear nuevo producto.

## Validación de transición de estado (código esperado)

```csharp
// En ProductService / Command handler:
private static bool IsValidTransition(ProductStatus from, ProductStatus to)
{
    return (from, to) switch
    {
        (ProductStatus.Draft,    ProductStatus.Active)    => true,
        (ProductStatus.Active,   ProductStatus.Archived)  => true,
        (ProductStatus.Archived, ProductStatus.Active)    => true,
        _ => false
    };
}
```

## Eventos auditables

| Evento | Trigger | Campos a loguear | Estado |
|---|---|---|---|
| ProductCreated | Create() | TenantId, ProductId, Slug, Sku, Price | ⏳ Pendiente; fuera de F5-03 |
| ProductPriceChanged | UpdateDetails() con precio distinto | ProductId, OldPrice, NewPrice | ✅ Implementado como `Product.ChangePrice` |
| ProductStatusChanged | ChangeStatus() | ProductId, OldStatus, NewStatus | ✅ Implementado como `Product.ChangeStatus` |

La auditoría es best-effort: `IAuditLogger` absorbe sus propios errores y nunca revierte una operación de negocio ya persistida.
