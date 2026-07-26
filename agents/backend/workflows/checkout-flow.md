# Workflow — Checkout Flow

> Flujo completo: Carrito → Pedido → Pago → Confirmación.
> Redefinido 2026-07-26 contra los patrones reales de Tenants/Store. Ver `domain/carts.md`,
> `domain/orders.md` y `domain/payments.md` para el detalle de cada pieza.
>
> **Estado real (2026-07-26): Pasos 1, 2 y 4 (Carrito → Checkout → Webhook) implementados y
> verificados en vivo.** El Paso 3 (pago en un provider externo REAL) sigue siendo diseño — hoy se
> prueba con `FakePaymentProviderAdapter` (dev-only, siempre exitoso), no con Bancard/PagoPar
> reales, bloqueados sin su documentación de API. Ver tabla "Estado de implementación" al final.

## Actores

| Actor | Rol |
|---|---|
| Buyer | Comprador anónimo (MVP) |
| Backend eShopy | Procesa carrito, pedido y pago |
| Payment Provider | Bancard / PagoPar |

## Diagrama de flujo

```
Buyer                         Backend                     Payment Provider
  │                              │                               │
  │ GET /api/store               │                               │
  │─────────────────────────────>│                               │
  │<── StorePublicDto ───────────│                               │
  │                              │                               │
  │ GET /api/public/products     │                               │
  │─────────────────────────────>│                               │
  │<── PagedResult<ProductPublicDto> ──────────────────────────  │
  │                              │                               │
  │ POST /api/cart/items         │ (CartToken generado en cliente)
  │ CartToken en header          │                               │
  │─────────────────────────────>│                               │
  │<── CartDto ─────────────────│                               │
  │                              │                               │
  │ [más items...]               │                               │
  │                              │                               │
  │ GET /api/cart                │                               │
  │─────────────────────────────>│                               │
  │<── CartDto con items ───────│                               │
  │                              │                               │
  │ POST /api/checkout           │                               │
  │ { buyerInfo, cartToken }     │                               │
  │─────────────────────────────>│                               │
  │                              │ Validar productos activos     │
  │                              │ Calcular total (snapshot)     │
  │                              │ Generar OrderNumber           │
  │                              │ Crear Order(PendingPayment)   │
  │                              │ Crear Payment(Initiated)      │
  │                              │──── Iniciar pago ────────────>│
  │                              │<─── paymentUrl ───────────────│
  │<── { orderId, paymentUrl } ──│                               │
  │                              │                               │
  │ Buyer accede a paymentUrl    │                               │
  │                              │      POST /api/payments/webhooks/{provider}
  │                              │<─────────────────────────────│
  │                              │ Validar firma                 │
  │                              │ Idempotencia                  │
  │                              │ Payment → Captured            │
  │                              │ Order → Paid                  │
  │                              │──────────────────────────────>│ 200
  │                              │                               │
  │ GET /checkout/success?orderId=...                            │
  │─────────────────────────────>│                               │
  │<── Detalle del pedido ───────│                               │
```

## Paso 1: Carrito

**CartToken**: UUID generado en el cliente (frontend). Enviado en header `X-Cart-Token` o query param.

```
POST /api/cart/items
Headers: X-Cart-Token: <uuid>
Body: { "productId": "...", "quantity": 2 }

Reglas:
- Producto debe estar Active en este tenant
- StockOnHand no se valida estrictamente en MVP (sin reserva de stock)
- Si CartToken no existe: crear Cart nuevo con ese token
- Si item existe: acumular cantidad o retornar error

GET /api/cart
Headers: X-Cart-Token: <uuid>
→ CartDto { items: [...], subtotal, currencyCode }
```

## Paso 2: Checkout (crear Order)

**Endpoint**: `POST /api/checkout`
**Auth**: Anónimo

**Request** (`cartToken` va en el header `X-Cart-Token`, igual que en el carrito — no en el body):
```
Headers: X-Cart-Token: <uuid>
```
```json
{
  "buyerEmail": "comprador@email.com",
  "buyerName": "María García",
  "shippingAddress": "Av. España 1234, Asunción"
}
```

**Acciones en backend** (orden real, ver `domain/orders.md` "Orden de llamada al provider de pago"
para el porque de este orden especifico):
```
1. Obtener Cart por CartToken + TenantId
2. Para cada CartItem:
   a. Obtener Product (Status = Active, por TenantId)
   b. Si no existe o no está Active: retornar PRODUCT_NOT_AVAILABLE (409)
3. Snapshot de precio: UnitPrice = Product.Price actual
4. Calcular TotalAmount = Σ(UnitPrice * Quantity)
5. Construir Order(PendingPayment) + OrderItems en memoria (Id generado client-side, sin OrderNumber todavia)
6. Llamar adapter.InitiateAsync(order.Id, TotalAmount, ...) — ANTES de escribir en la DB local
7. Construir Payment(Initiated) con la respuesta del provider (ProviderPaymentId, paymentUrl)
8. ICheckoutWriter.CreateAsync(...): genera OrderNumber (TenantCounter como concurrency token EF, sin SQL crudo) y persiste
   Order + OrderItems + Payment en una sola transaccion
9. Retornar { orderId, orderNumber, totalAmount, paymentUrl }
```

**Response 200:**
```json
{
  "orderId": "...",
  "orderNumber": 1042,
  "totalAmount": 170000,
  "currencyCode": "PYG",
  "paymentUrl": "https://pago.bancard.com.py/..."
}
```

> Hoy `paymentUrl` es sintetica (`https://fake-payment.local/pay/...`), generada por
> `FakePaymentProviderAdapter` — no hay Bancard/PagoPar real todavia (Fase 8).

## Paso 3: Pago en provider externo

- Buyer es redirigido a `paymentUrl`
- Completa el pago en Bancard/PagoPar (real, cuando exista) o en el flujo simulado de
  `FakePaymentProviderAdapter` (dev, hoy)
- Provider redirige al buyer a la URL de retorno (success/failed)
- Provider envía webhook asíncrono al backend

## Paso 4: Webhook de confirmación — implementado y verificado en vivo

**Endpoint**: `POST /api/payments/webhooks/{provider}` (excluido de TenantResolutionMiddleware)
**Auth**: Validación de firma del provider, delegada a `IPaymentProviderAdapter.ValidateWebhookSignature`

```
1. Validar firma/secret (header y formato propios de cada adapter — el controller pasa el body/headers crudos, no decide el formato)
2. Extraer EventId del payload (IPaymentProviderAdapter.ParseWebhook)
3. Verificar que (Provider, EventId) no exista en PaymentEventsProcessed — si ya existe, 200 sin reprocesar
4. Buscar Payment por (Provider, ProviderPaymentId) — sin tenant conocido, busca en todos los tenants
   (seguro por el Global Query Filter, ver domain/payments.md)
5. Fijar TenantContext al payment.TenantId encontrado (sin subdominio)
6. Según tipo de evento:
   - Captured: Payment → Captured, Order → Paid
   - Failed: Payment → Failed, Order → Cancelled
   - Refunded: Payment → Refunded, Order → Refunded
7. IPaymentWebhookWriter.ApplyAsync(...): persiste Payment + Order + el registro en PaymentEventsProcessed atomicamente
8. Retornar 200 OK (siempre que la firma sea valida y el Payment se encuentre, incluso en eventos ya procesados)
```

> Con `fake` (dev-only, ver `domain/payments.md`): header `X-Fake-Signature` = constante
> `FakePaymentProviderAdapter.WebhookSecret`, body `{ eventId, providerPaymentId, eventType }`. No es
> el formato de ningun provider real — Bancard/PagoPar tendran su propio header y payload cuando se
> construyan esos adapters.

## Paso 5: Resultado del pago (buyer)

**Éxito**: Buyer ve pantalla de confirmación con OrderNumber y detalles.
**Fallo**: Buyer ve pantalla de error con opción de reintentar.

El frontend consulta `GET /api/orders/{orderId}` para obtener el estado actual.

## Reglas críticas del flujo

| Regla | Consecuencia si se viola |
|---|---|
| Snapshot de precio en OrderItem | Pedidos históricos con precio incorrecto |
| Idempotencia de webhooks | Doble cobro o doble confirmación |
| Validar firma del webhook | Fraude: pedidos marcados como pagados sin pago real |
| OrderNumber atómico (concurrency token + reintento, sin SQL crudo) | Números de pedido duplicados en concurrencia |
| Verificar Product.Status = Active | Buyer compra producto archivado/Draft |

## Estado de implementación

| Componente | Estado |
|---|---|
| Cart | ✅ Implementado y verificado en vivo (Fase 6), ver `domain/carts.md` |
| Checkout / Order | ✅ Implementado y verificado en vivo (Fase 7, 2026-07-26), incluye el `Payment` inicial (`FakePaymentProviderAdapter`). Ver `domain/orders.md` — incluye el bug de concurrencia real encontrado y corregido en el smoke test |
| Payment / Webhook | ✅ Webhook, idempotencia (`PaymentEventsProcessed`) y transiciones implementados y verificados en vivo (2026-07-26). ❌ Adapters reales Bancard/PagoPar — bloqueados sin su documentación de API. Ver `domain/payments.md` |
| Productos (catálogo público) | ✅ Implementado |
| Store info | ✅ Implementado (`GET/PUT /api/store` reales, ya no hardcodeado) |
| Tenant / Onboarding / Usuarios | ✅ Implementado (Fase 4 completa) |
