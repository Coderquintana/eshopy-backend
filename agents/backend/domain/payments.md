# Domain — Payments

> Entidades `Payment` + `PaymentEventsProcessed`: transacciones y webhooks idempotentes.
> Redefinido 2026-07-26: agrega como el webhook resuelve el tenant sin subdominio (no existia
> respuesta a esto en el diseño original) y acota el alcance de los adapters reales.

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
  4. Buscar Payment por (Provider, ProviderPaymentId) — SIN tenant conocido de antemano (ver abajo)
  5. Fijar TenantContext a payment.TenantId (recien encontrado)
  6. Actualizar PaymentStatus según evento
  7. Actualizar OrderStatus según nueva tabla de transiciones
  8. Insertar (Provider, EventId) en PaymentEventsProcessed
  9. Retornar 200 OK
```

## Resolucion de tenant en el webhook (sin subdominio)

`/api/payments/webhooks/{provider}` ya esta correctamente excluido de `TenantResolutionMiddleware`
(el provider no envia un Host que matchee un subdominio nuestro). Eso significa que el paso 4 de
arriba debe buscar `Payment` **a traves de todos los tenants**, algo que ningun otro endpoint hace
hoy.

Esto es seguro gracias al Global Query Filter tal como quedo despues del fix de hoy en
`EShopyDbContext` (bug encontrado en el smoke test real, ver `BACKLOG.md` C-39):

```csharp
.HasQueryFilter(p => tenantContext.TenantId == null || p.TenantId == tenantContext.TenantId);
```

Con `TenantContext.TenantId` sin fijar (null), el filtro es transparente — la query busca en todos
los tenants sin necesitar `IgnoreQueryFilters()`. Una vez encontrado el `Payment`, el handler debe
fijar el tenant **antes** de tocar `Order` (que si esta filtrado):

```csharp
tenantContext.Set(payment.TenantId);  // sin subdominio — requiere el cambio de abajo
```

Esto requiere ampliar `EShopy.Application/Common/Context/TenantContext.cs`:
`Set(Guid tenantId, string? subdomain = null)` — el path de `TenantResolutionMiddleware` sigue
pasando el subdominio real; el path del webhook no tiene uno y no lo necesita.

## Proveedores soportados

| Provider | Tipo | Estado |
|---|---|---|
| Bancard | Pasarela local Paraguay | ❌ No implementado — requiere su API real, no se fabrica el contrato sin la documentacion del provider |
| PagoPar | Pasarela local Paraguay | ❌ No implementado — idem |
| `FakePaymentProviderAdapter` | Dev-only | Alcance de esta fase: implementa `IPaymentProviderAdapter` siempre exitoso, permite probar el flujo completo (checkout → webhook → Order Paid) sin credenciales reales. Mismo espiritu que los fakes en `EShopy.Tests.Integration/Support/` |

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

❌ **No implementado.** Planificado en Fase 8 del backlog. Diseño redefinido 2026-07-26: puerto,
webhook e idempotencia listos para implementar con `FakePaymentProviderAdapter`; los adapters reales
de Bancard/PagoPar quedan bloqueados hasta tener su documentacion de API.
