# Workflow — Product Lifecycle

> Flujo completo del ciclo de vida de un producto: Draft → Active → Archived.

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
6. Retorna ProductAdminDto con Status = "Draft"
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

```
1. Admin envía { "status": 1 } (Active)
2. ProductService:
   a. GetByIdAsync — si no existe → NOT_FOUND (404)
   b. Valida transición: Draft → Active ✅
   c. product.ChangeStatus(Active, utcNow)
   d. IProductRepository.UpdateAsync(product)
   e. AuditLog del cambio de estado (pendiente implementar)
3. Retorna ProductAdminDto con Status = "Active"
```

**Efecto en Storefront**: producto aparece en `GET /api/public/products`

## Flujo 3: Archivar producto (Active → Archived)

**Actor**: Tenant Admin/Owner
**Endpoint**: `PATCH /api/products/{id}/status`
**Auth**: `CatalogWrite`

```
1. Admin envía { "status": 2 } (Archived)
2. ProductService:
   a. GetByIdAsync — si no existe → NOT_FOUND (404)
   b. Valida transición: Active → Archived ✅
   c. product.ChangeStatus(Archived, utcNow)
   d. UpdateAsync
3. Retorna ProductAdminDto con Status = "Archived"
```

**Efecto en Storefront**: producto desaparece del catálogo público.
**Nota**: los OrderItems existentes conservan su snapshot — el archivado no afecta pedidos pasados.

## Flujo 4: Reactivar producto (Archived → Active)

Igual que Flujo 2 pero el producto parte desde `Archived`.

```
1. Admin envía { "status": 1 } (Active)
2. Valida transición: Archived → Active ✅
3. product.ChangeStatus(Active, utcNow) + UpdateAsync
```

## Flujo 5: Actualizar detalles de producto

**Actor**: Tenant Admin/Owner
**Endpoint**: `PUT /api/products/{id}`
**Auth**: `CatalogWrite`

```
1. Admin envía UpdateProductRequest (name, description, price, stockOnHand, sku?)
2. ProductService:
   a. GetByIdAsync — si no existe → NOT_FOUND (404)
   b. Si Sku cambia: verificar unicidad en tenant
   c. product.UpdateDetails(...)
   d. UpdateAsync
   e. AuditLog si el precio cambió (pendiente implementar)
3. Retorna ProductAdminDto actualizado
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

## Eventos auditables (AuditLog — pendiente Fase 9)

| Evento | Trigger | Campos a loguear |
|---|---|---|
| ProductCreated | Create() | TenantId, ProductId, Slug, Sku, Price |
| ProductPriceChanged | UpdateDetails() con precio distinto | ProductId, OldPrice, NewPrice |
| ProductStatusChanged | ChangeStatus() | ProductId, OldStatus, NewStatus |
