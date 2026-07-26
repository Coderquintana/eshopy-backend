# Domain — Carts

> Entidades `Cart` + `CartItem`: carrito server-side, previo al checkout. Multi-tenant.
> Redefinido 2026-07-26 contra los patrones reales de Tenants/Store — antes vivia repartido entre
> `workflows/checkout-flow.md` y `architecture/database-schema.md`, sin doc propio.

## Cart — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK |
| `TenantId` | `Guid` | No | FK tenant. Global Query Filter |
| `CartToken` | `string` | No | UUID generado en el **frontend**, no en backend |
| `ExpiresAtUtc` | `DateTime` | No | Para limpieza de carritos abandonados (F6-04, background job) |
| — columnas AppEntity — | | | CreatedAtUtc, UpdatedAtUtc, RowVersion, etc. |

## CartItem — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK |
| `CartId` | `Guid` | No | FK a Cart |
| `ProductId` | `Guid` | No | FK a Product |
| `Quantity` | `int` | No | `>= 1` |
| `CreatedAtUtc` / `UpdatedAtUtc` | `DateTime` | No/Sí | Timestamps propios (no hereda AppEntity: se resuelve via `CartId`, igual que `OrderItem` via `OrderId`) |

> `CartItem` **no** guarda `UnitPrice`: el precio se lee en vivo desde `Product` cada vez que se
> muestra el carrito. El snapshot de precio ocurre recien en el checkout, sobre `OrderItem` — un
> carrito no es un compromiso de precio, un pedido si.

## Reglas de dominio

- `CartToken` único por tenant (no global) — ver "Índices DB" abajo.
- Agregar un `ProductId` que ya está en el carrito **acumula** la cantidad, no duplica la fila.
- El producto debe estar `Status = Active` en este tenant para poder agregarse (validado en el
  comando, no es un constraint de DB — un producto puede archivarse despues de agregado sin romper
  el carrito existente, se re-valida recien en checkout).
- `Quantity` no valida contra `StockOnHand` en MVP — misma decisión ya tomada para `Order`
  (`domain/orders.md`): "stock no se reserva, solo se valida al checkout". No reabrir sin
  justificación fuerte.
- Un `Cart` vacío (sin `CartItem`s) es válido — no se borra automáticamente al quedar vacío.

## Índices DB

| Tabla | Índice | Tipo |
|---|---|---|
| Carts | `(TenantId, CartToken)` | UNIQUE — **no** `CartToken` global: mas restrictivo de lo necesario y filtraria colisiones entre tenants |
| CartItems | `(CartId, ProductId)` | UNIQUE — hace explicito a nivel DB la regla "un producto, una fila" |

## Endpoints asociados

Ver `architecture/api-contracts.md` para el detalle completo cuando se implemente.

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/cart` | Anónimo | Obtener el carrito actual (header `X-Cart-Token`) |
| POST | `/api/cart/items` | Anónimo | Agregar item — `{ productId, quantity }` |
| PUT | `/api/cart/items/{productId:guid}` | Anónimo | Actualizar cantidad de un item |
| DELETE | `/api/cart/items/{productId:guid}` | Anónimo | Quitar un item |

> Nota de redefinición: el diseño original decia `/api/cart/items/{id}` sin aclarar si `id` era el
> `CartItem.Id` interno o el `ProductId`. Se define como `ProductId`: el frontend siempre conoce el
> producto, nunca necesita enterarse del `CartItem.Id` interno — evita un GET previo solo para
> obtener un id que no le sirve para nada mas.

## CartDto (forma de respuesta)

```csharp
public record CartDto(
    string CartToken,
    IReadOnlyList<CartItemDto> Items,
    decimal Subtotal,
    string CurrencyCode
);

public record CartItemDto(
    Guid ProductId, string ProductName, string ProductSlug,
    decimal UnitPrice,  // leido en vivo desde Product, no un snapshot
    int Quantity, decimal Subtotal
);
```

## Estado de implementación

✅ **Implementado y verificado en vivo** (2026-07-26) contra SQL Server real: acumular cantidad,
listar, actualizar, eliminar — todo probado end-to-end, sin bugs. `Cart` es el primer agregado del
proyecto con una coleccion hija encapsulada (`Items` de solo lectura respaldada por un campo privado,
mapeada con `PropertyAccessMode.Field` en `CartConfiguration`).

Ver `workflows/checkout-flow.md` para el flujo completo (Carrito → Pedido → Pago). Fase 7 (Pedidos)
es el siguiente paso — depende de Cart.
