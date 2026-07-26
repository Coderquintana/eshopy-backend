# Domain — Orders

> Entidades `Order` + `OrderItem`: pedido generado desde checkout. Multi-tenant.
> Redefinido 2026-07-26: agrega el mecanismo real de escritura atomica (`ICheckoutWriter`) y el
> orden de llamada al provider de pago, ninguno de los dos existia cuando este doc se escribio.

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

`OrderNumber` **no** se conoce al crear el `Order` en memoria — se genera atomicamente recien al
persistir (ver "Escritura atomica" abajo). Por eso el factory no lo recibe, y existe un metodo
separado para asignarlo despues:

```csharp
Order.Create(
    tenantId: Guid,
    storeId: Guid,
    buyerEmail: string,
    buyerName: string,
    shippingAddress: string?,
    cartToken: string,
    items: IEnumerable<OrderItemData>,  // snapshot del carrito, ya con UnitPrice actual de Product
    currencyCode: string,
    createdAtUtc: DateTime
) → Order (Status = PendingPayment, OrderNumber = 0/no asignado)

order.AssignOrderNumber(int orderNumber)  // llamado UNA vez, por ICheckoutWriter
```

## Escritura atomica (`ICheckoutWriter`)

Checkout escribe a traves de **cuatro** cosas a la vez: `Order`, sus `OrderItem`s, el `Payment`
inicial, y el incremento de `TenantCounters` — el mismo tipo de operacion que ya resolvimos para el
onboarding de tenants (`ITenantOnboardingWriter`, ver `GOVERNANCE.md`). Se define un writer angosto
equivalente:

```csharp
public interface ICheckoutWriter
{
  // Devuelve el OrderNumber generado atomicamente. Asigna order.AssignOrderNumber(...) internamente
  // antes de persistir.
  Task<int> CreateAsync(Order order, IReadOnlyList<OrderItem> items, Payment payment, CancellationToken ct);
}
```

A diferencia de `EfTenantOnboardingWriter` (que solo hace `Add` + un `SaveChangesAsync`), la
implementacion de este writer mezcla SQL crudo sobre `TenantCounters`
(`UPDATE ... WITH (UPDLOCK, ROWLOCK) ... OUTPUT INSERTED.CurrentValue`) con entidades trackeadas por
EF Core — **necesita una transaccion explicita** (`db.Database.BeginTransactionAsync()`), la primera
vez que hace falta en el proyecto: hasta ahora un solo `SaveChangesAsync` alcanzaba siempre porque
nunca se combino SQL crudo con `Add()` en la misma operacion.

## Orden de llamada al provider de pago

Mismo principio ya establecido para Keycloak en el onboarding (`CreateTenantCommandHandler`): llamar
al sistema externo **antes** de escribir localmente, para no dejar huerfanos locales si el externo
falla.

1. Construir `Order` + `OrderItem`s en memoria (`Id` generado client-side, como ya hace
   `Product.Create`) — todavia sin `OrderNumber`.
2. Llamar `IPaymentProviderAdapter.InitiateAsync(order.Id, amount, currency, ...)` usando `order.Id`
   (`Guid`) como referencia de comercio — **no** `OrderNumber`, que todavia no existe. Esto desacopla
   al provider del contador atomico.
3. Construir `Payment(Initiated)` con la respuesta del provider.
4. Recien ahi, `ICheckoutWriter.CreateAsync(...)` — escritura local atomica que genera `OrderNumber`
   y persiste `Order` + `OrderItem`s + `Payment` juntos.

**Trade-off aceptado** (igual que en onboarding, documentado explicitamente, no un descuido): si el
paso 4 falla despues de que el provider ya inicio el pago en el paso 2, queda un huerfano del lado
del provider, no en nuestra DB. No se intenta resolver con un saga en este alcance — el huerfano en
el provider es recuperable manualmente (soporte/ops), un huerfano local silencioso no lo es.

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

❌ **No implementado.** Planificado en Fase 7 del backlog. Diseño redefinido 2026-07-26 (ver nota al
inicio del doc) — listo para implementar, no requiere otra pasada de diseño.
