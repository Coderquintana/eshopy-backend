# Domain — Orders

> Entidades `Order` + `OrderItem`: pedido generado desde checkout. Multi-tenant.

## Order — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK |
| `TenantId` | `Guid` | No | FK tenant |
| `StoreId` | `Guid` | No | FK store del tenant |
| `OrderNumber` | `int` | No | Secuencial por tenant. Generado con TenantCounters (UPDLOCK) |
| `Status` | `OrderStatus` | No | Ver tabla de estados |
| `BuyerEmail` | `string` | No | Email del comprador al momento del checkout |
| `BuyerName` | `string` | No | Nombre del comprador |
| `ShippingAddress` | `string` | Sí | Dirección de entrega (JSON o texto) |
| `TotalAmount` | `decimal` | No | Suma de OrderItems. Snapshot |
| `CurrencyCode` | `string` | No | Heredado del Store |
| `CartToken` | `string` | No | CartToken del carrito origen |
| `PaymentId` | `Guid?` | Sí | FK a Payment (null hasta que se inicia pago) |
| — columnas AppEntity — | | | CreatedAtUtc, UpdatedAtUtc, etc. |

## OrderItem — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK |
| `OrderId` | `Guid` | No | FK a Order |
| `ProductId` | `Guid` | No | FK al producto (referencia histórica) |
| `ProductName` | `string` | No | **Snapshot** del nombre al momento del checkout |
| `ProductSku` | `string?` | Sí | **Snapshot** del SKU |
| `UnitPrice` | `decimal` | No | **Snapshot** del precio unitario |
| `Quantity` | `int` | No | `>= 1` |
| `Subtotal` | `decimal` | No | `UnitPrice * Quantity` |

> Los snapshots en OrderItem son inmutables una vez creados. Un cambio de precio posterior en Product no afecta pedidos existentes.

## Estados y transiciones (OrderStatus)

| Estado | Valor | Descripción |
|---|---|---|
| `PendingPayment` | 0 | Creado, esperando confirmación de pago |
| `Paid` | 1 | Pago confirmado por webhook |
| `Cancelled` | 2 | Cancelado (pago fallido o expirado) |
| `Refunded` | 3 | Reembolsado |

| Desde → Hacia | Permitida | Condición |
|---|---|---|
| PendingPayment → Paid | ✅ | Requiere Payment con Status = Captured |
| PendingPayment → Cancelled | ✅ | Pago fallido o timeout |
| Paid → Refunded | ✅ | Webhook de refund del provider |
| Paid → Cancelled | ❌ | — |
| Cancelled → * | ❌ | Estado terminal |
| Refunded → * | ❌ | Estado terminal |

## Reglas de dominio

- `OrderNumber` es secuencial por tenant. Usar `TenantCounters` con `UPDLOCK/ROWLOCK` para evitar duplicados en concurrencia.
- `TotalAmount` = suma de todos los `OrderItem.Subtotal`. Calculado al crear.
- Un `Order` no puede pasar a `Paid` sin un `Payment` con `Status = Captured`.
- `OrderItems` son inmutables una vez que la Order está creada.
- Stock no se decrementa al crear Order (solo reserva conceptual en MVP). Post-MVP: gestión de inventario.

## Factory method (diseño esperado)

```csharp
Order.Create(
    tenantId: Guid,
    storeId: Guid,
    orderNumber: int,          // generado por TenantCounters
    buyerEmail: string,
    buyerName: string,
    cartToken: string,
    items: IEnumerable<OrderItemData>,  // snapshot del carrito
    currencyCode: string,
    createdAtUtc: DateTime
) → Order (Status = PendingPayment)
```

## Índices DB

| Índice | Tipo |
|---|---|
| `(TenantId, OrderNumber)` | UNIQUE |
| `(TenantId, Status)` | IX |
| `(TenantId, BuyerEmail)` | IX |

## Casos de uso asociados

| UC | Nombre |
|---|---|
| UC-06 | Checkout — crea Order desde Cart |
| UC-07 | Iniciar pago — crea Payment asociado |
| UC-08 | Confirmar pago — webhook actualiza Order a Paid |
| UC-09 | Administrar pedidos — admin lista/detalla Orders |

## Estado de implementación

❌ **No implementado.** Planificado en Fase 7 del backlog.
