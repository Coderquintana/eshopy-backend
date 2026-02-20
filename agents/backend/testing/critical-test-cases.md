# Testing — Critical Test Cases

> Casos de test obligatorios antes de producción. Organizados por módulo.

## Formato

Cada caso incluye: **Given** (contexto), **When** (acción), **Then** (resultado esperado).

---

## Módulo: Multi-tenancy (CRÍTICO)

| ID | Given | When | Then |
|---|---|---|---|
| MT-01 | Producto creado para tenantA | tenantB consulta lista de productos | tenantB no ve ningún producto de tenantA |
| MT-02 | Mismo slug `"remera"` en tenantA y tenantB | tenantA consulta `GET /api/products` | Solo retorna el producto de tenantA |
| MT-03 | Request sin tenant resuelto | Cualquier endpoint de negocio | 404 `TENANT_NOT_FOUND` |
| MT-04 | TenantId ausente en entidad | EF SaveChanges | Interceptor rechaza la operación |

---

## Módulo: Products — Dominio

| ID | Given | When | Then |
|---|---|---|---|
| PD-01 | Datos válidos | `Product.Create(...)` | Status = Draft, Slug en lowercase, Sku en uppercase |
| PD-02 | Price = -1 | `Product.Create(...)` | `DomainException` con código `VALIDATION_ERROR` |
| PD-03 | StockOnHand = -5 | `Product.Create(...)` | `DomainException` |
| PD-04 | Slug vacío | `Product.Create(...)` | `DomainException` |
| PD-05 | SKU de 65 chars | `Product.Create(...)` | `DomainException` |
| PD-06 | Price = 0 | `Product.Create(...)` | Producto creado (precio 0 es válido) |

---

## Módulo: Products — Transiciones de estado

| ID | Given | When | Then |
|---|---|---|---|
| PS-01 | Producto en Draft | ChangeStatus(Active) | Status = Active, visible en Storefront |
| PS-02 | Producto en Draft | ChangeStatus(Archived) | `PRODUCT_INVALID_STATE` (409) |
| PS-03 | Producto en Active | ChangeStatus(Archived) | Status = Archived, no visible en Storefront |
| PS-04 | Producto en Active | ChangeStatus(Draft) | `PRODUCT_INVALID_STATE` (409) |
| PS-05 | Producto en Archived | ChangeStatus(Active) | Status = Active, visible en Storefront |
| PS-06 | Producto en Archived | ChangeStatus(Draft) | `PRODUCT_INVALID_STATE` (409) |

---

## Módulo: Products — API y seguridad

| ID | Given | When | Then |
|---|---|---|---|
| PA-01 | Sin token de auth | `POST /api/products` | 401 `UNAUTHORIZED` |
| PA-02 | Token válido sin `catalog.write` | `POST /api/products` | 403 `FORBIDDEN` |
| PA-03 | Token válido con `catalog.write` | `POST /api/products` con body válido | 201 con ProductAdminDto |
| PA-04 | Sin token | `GET /api/public/products` | 200 (anónimo) |
| PA-05 | Slug duplicado en mismo tenant | `POST /api/products` con slug existente | 409 `CONFLICT` |
| PA-06 | SKU duplicado en mismo tenant | `POST /api/products` con sku existente | 409 `CONFLICT` |
| PA-07 | Slug duplicado en **otro** tenant | `POST /api/products` con mismo slug | 201 (no hay conflicto) |
| PA-08 | ID inexistente | `GET /api/products/{id}` | 404 `NOT_FOUND` |

---

## Módulo: Concurrencia

| ID | Given | When | Then |
|---|---|---|---|
| CC-01 | Mismo producto | Dos `PUT /api/products/{id}` simultáneos | Uno retorna 409 (RowVersion conflict) |
| CC-02 | Mismo tenant | Dos checkouts simultáneos | OrderNumbers distintos (sin duplicados) |

---

## Módulo: Checkout y Orders (implementar cuando estén disponibles)

| ID | Given | When | Then |
|---|---|---|---|
| CO-01 | Carrito con producto Active | POST /api/checkout con buyerInfo válido | Order creada, Status = PendingPayment, paymentUrl retornada |
| CO-02 | Carrito con producto Archived | POST /api/checkout | 409 `PRODUCT_NOT_AVAILABLE` |
| CO-03 | Carrito vacío | POST /api/checkout | 400 `VALIDATION_ERROR` |
| CO-04 | Order en PendingPayment | ChangeStatus(Paid) sin Payment Captured | Error — violación de regla |
| CO-05 | Order Paid | ChangeStatus(Cancelled) | `ORDER_INVALID_STATE` (409) |

---

## Módulo: Payments y Webhooks (implementar cuando estén disponibles)

| ID | Given | When | Then |
|---|---|---|---|
| PW-01 | EventId nuevo | Webhook Captured | Payment → Captured, Order → Paid |
| PW-02 | EventId ya procesado | Mismo webhook enviado de nuevo | 200 OK, estado sin cambiar |
| PW-03 | Firma inválida en header | Webhook con secret incorrecto | 401 `PAYMENT_WEBHOOK_INVALID` |
| PW-04 | Monto del webhook distinto al Order | Webhook Captured | Error / log de alerta |
| PW-05 | Webhook Failed | Webhook de rechazo | Payment → Failed, Order → Cancelled |

---

## Módulo: Subscriptions y Tenant Lifecycle (implementar cuando estén disponibles)

| ID | Given | When | Then |
|---|---|---|---|
| SL-01 | Subdomain existente | POST /api/onboarding/tenants con mismo subdomain | 409 `SUBDOMAIN_ALREADY_EXISTS` |
| SL-02 | Tenant PendingPayment | Webhook de pago de suscripción exitoso | Tenant → Active, Subscription → Active |
| SL-03 | Tenant Active, renovación fallida | Webhook de pago fallido | Subscription → PastDue |
| SL-04 | Tenant Suspended | Request a endpoint del tenant | 402/403 — tenant suspendido |

---

## Checklist pre-producción

- [ ] MT-01 a MT-04 pasando
- [ ] PD-01 a PD-06 pasando
- [ ] PS-01 a PS-06 pasando
- [ ] PA-01 a PA-08 pasando
- [ ] CC-01, CC-02 pasando
- [ ] CO-01 a CO-05 pasando
- [ ] PW-01 a PW-05 pasando
- [ ] SL-01 a SL-04 pasando
- [ ] Tests de integración en CI pasando sin flakiness
