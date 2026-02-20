# Domain — Payments

> Entidades `Payment` + `PaymentEventsProcessed`: transacciones y webhooks idempotentes.

## Payment — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK interno |
| `TenantId` | `Guid` | No | FK tenant |
| `OrderId` | `Guid` | No | FK a Order |
| `Status` | `PaymentStatus` | No | Ver tabla de estados |
| `Provider` | `string` | No | `"bancard"` o `"pagopar"` |
| `ProviderPaymentId` | `string?` | Sí | ID de la transacción en el provider |
| `ProviderPaymentUrl` | `string?` | Sí | URL de pago devuelta al frontend |
| `Amount` | `decimal` | No | Monto de la transacción |
| `CurrencyCode` | `string` | No | Heredado del Store |
| `ErrorCode` | `string?` | Sí | Código de error del provider si falló |
| `ErrorMessage` | `string?` | Sí | Mensaje de error del provider |
| — columnas AppEntity — | | | CreatedAtUtc, UpdatedAtUtc, etc. |

## PaymentEventsProcessed — Idempotencia

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `Guid` | PK |
| `Provider` | `string` | Provider del webhook |
| `EventId` | `string` | ID único del evento en el provider |
| `ProcessedAtUtc` | `DateTime` | Cuándo se procesó |

> Antes de procesar cualquier webhook, verificar que `(Provider, EventId)` no exista en esta tabla.

## Estados y transiciones (PaymentStatus)

| Estado | Valor | Descripción |
|---|---|---|
| `Initiated` | 0 | Intención de pago creada; URL generada |
| `Authorized` | 1 | Fondos reservados (pre-captura) |
| `Captured` | 2 | Pago confirmado — dispara Order → Paid |
| `Failed` | 3 | Rechazado o fallido — dispara Order → Cancelled |
| `Refunded` | 4 | Reembolsado — dispara Order → Refunded |

| Desde → Hacia | Trigger |
|---|---|
| Initiated → Authorized | Webhook del provider |
| Initiated → Failed | Webhook de rechazo o timeout |
| Authorized → Captured | Webhook de captura |
| Authorized → Failed | Webhook de error |
| Captured → Refunded | Webhook de refund |

## Reglas de dominio

- Un `Payment` solo puede existir asociado a un `Order` en estado `PendingPayment`.
- El webhook debe validar firma/secret antes de procesar — nunca confiar ciegamente.
- Si el `EventId` ya está en `PaymentEventsProcessed`, retornar 200 sin reprocessar.
- Solo un `Payment` activo por `Order` en MVP (puede haber reintentos si el anterior falló).
- `Amount` debe coincidir con `Order.TotalAmount`. Validar en webhook.

## Flujo de webhook idempotente

```
POST /api/payments/webhooks/{provider}
  1. Validar firma/secret del header
  2. Extraer EventId del payload
  3. Verificar que (Provider, EventId) no exista en PaymentEventsProcessed
  4. Buscar Payment por ProviderPaymentId
  5. Actualizar PaymentStatus según evento
  6. Actualizar OrderStatus según nueva tabla de transiciones
  7. Insertar (Provider, EventId) en PaymentEventsProcessed
  8. Retornar 200 OK
```

## Proveedores soportados

| Provider | Tipo | Estado |
|---|---|---|
| Bancard | Pasarela local Paraguay | ❌ No implementado (Fase 8) |
| PagoPar | Pasarela local Paraguay | ❌ No implementado (Fase 8) |

## Contrato de adaptador

```csharp
public interface IPaymentProviderAdapter
{
    string Provider { get; }
    Task<InitiatePaymentResult> InitiateAsync(PaymentRequest request, CancellationToken ct);
    bool ValidateWebhookSignature(HttpRequest request, string secret);
    Task<WebhookEvent> ParseWebhookAsync(HttpRequest request, CancellationToken ct);
}
```

## Endpoints

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/payments` | CatalogWrite* | Iniciar pago. Retorna `paymentUrl` |
| POST | `/api/payments/webhooks/{provider}` | Firma provider | Webhook idempotente |

## Estado de implementación

❌ **No implementado.** Planificado en Fase 8 del backlog.
