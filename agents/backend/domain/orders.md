# Domain — Orders

> Entidades `Order` + `OrderItem`: pedido generado desde checkout. Multi-tenant.
> Implementado y verificado en vivo 2026-07-26 (Fase 7). El smoke test contra SQL Server real con
> checkouts concurrentes encontro un bug real en la escritura atomica — ver "Escritura atomica" abajo,
> seccion actualizada con el diseño final (distinto del disenado antes de implementar).

## Order — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK |
| `TenantId` | `Guid` | No | FK tenant |
| `StoreId` | `Guid` | No | FK store del tenant |
| `OrderNumber` | `int` | No | Secuencial por tenant. Generado con TenantCounters (concurrency token + reintento, sin SQL crudo) |
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

- `OrderNumber` es secuencial por tenant. Generado via `TenantCounters` con `CurrentValue` como concurrency token EF + reintento (ver "Escritura atomica" abajo) — sin SQL crudo.
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

order.AssignOrderNumber(int orderNumber)  // ver nota de idempotencia abajo
```

> **Idempotente a proposito.** A diferencia del diseño original, `AssignOrderNumber` **no** tira si se
> llama mas de una vez sobre la misma instancia — simplemente sobreescribe. Es asi por el bug real
> descripto abajo: `EfCheckoutWriter` reintenta sobre la MISMA instancia de `Order` bajo contencion, y
> un guard de "solo una vez" rompia el reintento (tiraba `DomainException` en el segundo intento en vez
> de dejar que el writer reintente limpio).

## Escritura atomica (`ICheckoutWriter`)

Checkout escribe a traves de **tres** cosas a la vez: `Order` (con sus `OrderItem`s, ya viajan dentro
via la coleccion encapsulada `Order.Items` — no se pasan aparte, seria redundante), el `Payment`
inicial, y el incremento de `TenantCounters` — el mismo tipo de operacion que ya resolvimos para el
onboarding de tenants (`ITenantOnboardingWriter`, ver `GOVERNANCE.md`):

```csharp
public interface ICheckoutWriter
{
  // Asigna order.AssignOrderNumber(...) internamente y devuelve el numero generado.
  Task<int> CreateAsync(Order order, Payment payment, CancellationToken ct);
}
```

**Sin SQL crudo.** `TenantCounters` es una entidad EF normal (`TenantCounter`: `TenantId` + `CounterType`
como PK compuesta, `CurrentValue` como el resto de las entidades). `CurrentValue` se marca
`IsConcurrencyToken()` en la configuracion EF — asi el `UPDATE` que genera EF Core automaticamente
incluye `WHERE CurrentValue = @valorLeido`.

### Bug real encontrado en el smoke test de concurrencia (2026-07-26)

El diseño original asumia que bajo contencion, el perdedor de la carrera **siempre** recibe
`DbUpdateConcurrencyException` (0 filas afectadas en el `UPDATE` del counter). Disparando checkouts
concurrentes de verdad contra SQL Server real, eso NO fue lo que paso: varios requests fallaron con
una violacion de indice unico cruda (`SqlException` 2601 sobre `UQ_Orders_TenantId_OrderNumber`), no
con `DbUpdateConcurrencyException` — y como `EfCheckoutWriter` solo atrapaba esta ultima, el checkout
fallaba duro en vez de reintentar.

**Por que pasa:** el `UPDATE` del counter y el `INSERT` del `Order` viajan en el mismo batch/transaccion
de un `SaveChangesAsync`. Si el `UPDATE` de un checkout se bloquea esperando el lock de otro que esta
en vuelo, al desbloquearse su `WHERE CurrentValue = @original` ya no matchea (el ganador ya committeo)
— pero eso es un `UPDATE` que afecta 0 filas, **no un error SQL**. SQL Server sigue ejecutando el resto
del batch igual, incluido el `INSERT` de `Order` con el mismo `OrderNumber` que el ganador ya
confirmo — y **ese** choca contra el indice unico. El mismatch de filas afectadas (lo que dispara
`DbUpdateConcurrencyException` del lado de EF) recien se evalua despues, cuando ya es tarde.

**Fix:** el writer atrapa ambas excepciones y reintenta en los dos casos:

```csharp
for (var attempt = 0; attempt < MaxRetries; attempt++)
{
  var counter = await GetOrCreateCounterAsync(tenantId, ct);   // TRACKED
  counter.Increment();                                          // CurrentValue++
  order.AssignOrderNumber(counter.CurrentValue);                // idempotente, ver nota arriba

  db.Orders.Add(order);      // cascadea a order.Items via la navegacion EF
  db.Payments.Add(payment);

  try
  {
    await db.SaveChangesAsync(ct);
    return counter.CurrentValue;
  }
  catch (DbUpdateConcurrencyException)
  {
    db.ChangeTracker.Clear();  // perdio la carrera "limpio", reintentar
  }
  catch (DbUpdateException ex) when (ex.InnerException is SqlException sql
    && (sql.Number == 2601 || sql.Number == 2627))
  {
    db.ChangeTracker.Clear();  // perdio la carrera "sucio" (INSERT choco), mismo tratamiento
  }
}
throw new DomainException(ErrorCodes.ConcurrencyConflict, "No se pudo generar el numero de pedido.");
```

Verificado con 25 checkouts concurrentes reales contra SQL Server: 0 `OrderNumber` duplicados, 0
gaps, `TenantCounters.CurrentValue` consistente con el conteo real de `Orders`. Bajo contencion muy
alta sobre el mismo tenant (25 requests simultaneos contra un solo counter) una fraccion agota
`MaxRetries` (5) y recibe `CONCURRENCY_CONFLICT` (409) — resultado seguro, no corrupcion, el caller
puede reintentar. `GlobalExceptionMiddleware` solo ve esto si se agotan los intentos; el reintento
normal es interno e invisible para el caller.

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

✅ **Implementado y verificado en vivo** (2026-07-26) contra SQL Server real, incluyendo un test de
concurrencia real (25 checkouts simultaneos) que encontro y corrigio el bug descripto arriba. Incluye
el subconjunto minimo de Payments necesario para que Checkout funcione (`Payment` entidad,
`IPaymentProviderAdapter.InitiateAsync`, `FakePaymentProviderAdapter`) — el modulo completo de Payments
(webhook, idempotencia, adapters reales) sigue en Fase 8, ver `domain/payments.md`.
