# Domain — Payments

> Entidades `Payment` + `PaymentEventsProcessed`: transacciones y webhooks idempotentes.
> **Implementado y verificado en vivo 2026-07-26** (Fase 7 dejo el minimo para Checkout; Fase 8
> agrego el webhook completo el mismo dia). Solo quedan pendientes los adapters reales de
> Bancard/PagoPar — bloqueados hasta tener su documentacion de API (ver "Proveedores soportados").

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
> Implementado sin `TenantId`: el chequeo ocurre antes de que el tenant se conozca, y `EventId` ya es
> unico a nivel provider — no hace falta acotarlo por tenant.

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
| Initiated → Captured | Webhook de captura directa (implementado 2026-07-26): varios gateways de redirect no emiten un evento de autorizacion separado, solo confirman en un unico webhook — exigir el paso intermedio rompería ese caso real |
| Initiated → Failed | Webhook de rechazo o timeout |
| Authorized → Captured | Webhook de captura |
| Authorized → Failed | Webhook de error |
| Captured → Refunded | Webhook de refund |

## Reglas de dominio

- Un `Payment` solo puede existir asociado a un `Order` en estado `PendingPayment`.
- El webhook debe validar firma/secret antes de procesar — nunca confiar ciegamente. Implementado
  via `IPaymentProviderAdapter.ValidateWebhookSignature`.
- Si el `EventId` ya está en `PaymentEventsProcessed`, retornar 200 sin reprocessar. Implementado.
- Solo un `Payment` activo por `Order` en MVP (puede haber reintentos si el anterior falló).
- **Pendiente, no implementado todavia**: validar que el monto reportado por el webhook coincida con
  `Order.TotalAmount`. `WebhookEvent` hoy no lleva `Amount` — se agrega cuando se construya el primer
  adapter real (Bancard/PagoPar), una vez que se sepa que forma tiene ese dato en cada payload real;
  fabricarlo ahora seria adivinar un contrato sin caso de uso real que lo ejercite.

## Flujo de webhook idempotente

```
POST /api/payments/webhooks/{provider}
  1. Validar firma/secret del header
  2. Extraer EventId del payload
  3. Verificar que (Provider, EventId) no exista en PaymentEventsProcessed
  4. Buscar Payment por (Provider, ProviderPaymentId) — SIN tenant conocido de antemano (ver abajo)
  5. Fijar TenantContext a payment.TenantId (recien encontrado)
  6. Actualizar PaymentStatus según evento
  7. Actualizar OrderStatus según nueva tabla de transiciones
  8. Insertar (Provider, EventId) en PaymentEventsProcessed
  9. Retornar 200 OK
```

## Resolucion de tenant en el webhook (sin subdominio) — implementado

`/api/payments/webhooks/{provider}` esta excluido de `TenantResolutionMiddleware` (el provider no
envia un Host que matchee un subdominio nuestro). El paso 4 del flujo de arriba busca `Payment` **a
traves de todos los tenants** — el unico lugar del proyecto que hace esto.

Es seguro gracias al Global Query Filter (bug encontrado y corregido en el smoke test real de
Tenants, ver `BACKLOG.md` C-39):

```csharp
.HasQueryFilter(p => tenantContext.TenantId == null || p.TenantId == tenantContext.TenantId);
```

Con `TenantContext.TenantId` sin fijar (null), el filtro es transparente — la query busca en todos
los tenants sin necesitar `IgnoreQueryFilters()`. Una vez encontrado el `Payment`,
`ProcessPaymentWebhookCommandHandler` fija el tenant **antes** de tocar `Order` (que si esta
filtrado):

```csharp
tenantContext.Set(payment.TenantId); // sin subdominio
```

`EShopy.Application/Common/Context/TenantContext.cs` tiene `Set(Guid tenantId, string? subdomain =
null)` — el path de `TenantResolutionMiddleware` sigue pasando el subdominio real; el path del
webhook no tiene uno y no lo necesita.

## Proveedores soportados

| Provider | Tipo | Estado |
|---|---|---|
| Bancard | Pasarela local Paraguay | ❌ No implementado — requiere su API real, no se fabrica el contrato sin la documentacion del provider |
| PagoPar | Pasarela local Paraguay | ❌ No implementado — idem |
| `FakePaymentProviderAdapter` | Dev-only | ✅ Implementado y verificado en vivo: `InitiateAsync` siempre exitoso + `ValidateWebhookSignature`/`ParseWebhook` con un formato **propio, inventado** (no el de ningun provider real) — permite probar el flujo completo (checkout → webhook → Order Paid) sin credenciales reales. Firma: header `X-Fake-Signature` debe matchear la constante `FakePaymentProviderAdapter.WebhookSecret`. Payload: `{ "eventId", "providerPaymentId", "eventType": "Captured"\|"Failed"\|"Refunded" }` (case-sensitive) |

## Contrato de adaptador (implementado)

Corregido durante la implementacion: el diseño original tenia `ValidateWebhookSignature(HttpRequest
request, ...)`/`ParseWebhookAsync(HttpRequest request, ...)`, pero `EShopy.Application` no depende de
ASP.NET Core (mismo principio que el resto del proyecto). El controller lee el body crudo y los
headers, y se los pasa al adapter como texto/diccionario — cada adapter interpreta ese formato con la
convencion propia de su provider:

```csharp
public interface IPaymentProviderAdapter
{
    string Provider { get; }
    Task<InitiatePaymentResult> InitiateAsync(InitiatePaymentRequest request, CancellationToken ct);
    bool ValidateWebhookSignature(string rawBody, IReadOnlyDictionary<string, string> headers);
    WebhookEvent ParseWebhook(string rawBody);
}

public sealed record WebhookEvent(string EventId, string ProviderPaymentId, PaymentWebhookEventType EventType);
public enum PaymentWebhookEventType { Captured, Failed, Refunded }
```

Puede haber mas de un adapter registrado a la vez (uno por provider soportado); el handler del
webhook resuelve el que corresponda buscando por `Provider` sobre `IEnumerable<IPaymentProviderAdapter>`
— asi que agregar Bancard/PagoPar cuando haya documentacion es solo registrar una implementacion mas,
sin tocar el resto del flujo.

## Escritura atomica del webhook (`IPaymentWebhookWriter`)

Mismo espiritu que `ICheckoutWriter` — un writer angosto de un solo caso de uso, no un
`IUnitOfWork` generico (ver GOVERNANCE.md):

```csharp
public interface IPaymentWebhookWriter
{
  Task<Payment?> FindByProviderPaymentIdAsync(string provider, string providerPaymentId, CancellationToken ct);
  Task<bool> IsEventProcessedAsync(string provider, string eventId, CancellationToken ct);
  Task ApplyAsync(Payment payment, Order order, string provider, string eventId, DateTime processedAtUtc, CancellationToken ct);
}
```

`ApplyAsync` persiste `Payment` + `Order` + el registro en `PaymentEventsProcessed` en un solo
`SaveChangesAsync` — sin SQL crudo, sin transaccion explicita, mismo patron que el resto del
proyecto.

## Endpoints

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/payments/webhooks/{provider}` | Firma del provider (validada por el adapter) | Webhook idempotente |

> No existe un `POST /api/payments` independiente: `Payment` se crea internamente dentro de
> `POST /api/checkout` (ver `domain/orders.md`) — el diseño original lo sugeria como endpoint propio,
> pero no hay un caso de uso real que inicie un pago sin pasar por checkout.

## Estado de implementación

✅ **Implementado y verificado en vivo** (2026-07-26): webhook completo (validacion de firma,
idempotencia via `PaymentEventsProcessed`, resolucion de tenant sin subdominio, transiciones de
Payment/Order) probado contra SQL Server real — captura exitosa, fallo, reenvio del mismo EventId
(sin duplicar), firma invalida (401) y `ProviderPaymentId` desconocido (404). Los adapters reales de
Bancard/PagoPar quedan bloqueados hasta tener su documentacion de API — el puerto y el resto del
flujo ya estan listos para recibirlos.
