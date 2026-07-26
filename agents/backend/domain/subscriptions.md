# Domain — Subscriptions

> Entidad `Subscription`: suscripción mensual del tenant a un plan de eShopy.

## Subscription — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK |
| `TenantId` | `Guid` | No | FK tenant (relación 1:N, activa solo 1) |
| `Plan` | `string` | No | `"basic"`, `"gold"`, `"diamond"` |
| `Status` | `SubscriptionStatus` | No | Ver tabla de estados |
| `BillingCycleStart` | `DateTime` | No | Inicio del ciclo actual |
| `BillingCycleEnd` | `DateTime` | No | Fin del ciclo (renovación o vencimiento) |
| `PriceAmount` | `decimal` | No | Precio del plan al momento de la suscripción |
| `CurrencyCode` | `string` | No | Moneda del cobro |
| `ExternalSubscriptionId` | `string?` | Sí | ID en la plataforma de billing externa |
| `CancelledAtUtc` | `DateTime?` | Sí | Cuándo se canceló |
| — columnas AppEntity — | | | CreatedAtUtc, UpdatedAtUtc, etc. |

## Estados (SubscriptionStatus)

| Estado | Descripción |
|---|---|
| `PendingActivation` | Creada, esperando primer pago |
| `Active` | Activa y al día |
| `PastDue` | Pago fallido. Período de gracia |
| `Suspended` | Tenant suspendido por mora |
| `Cancelled` | Cancelada definitivamente |

| Desde → Hacia | Trigger |
|---|---|
| PendingActivation → Active | Primer pago exitoso (webhook) |
| Active → PastDue | Falla en renovación automática |
| PastDue → Active | Pago de regularización exitoso |
| PastDue → Suspended | Expiración período de gracia |
| Suspended → Active | Pago tardío aceptado |
| Active → Cancelled | Cancelación voluntaria del owner |
| Suspended → Cancelled | Cancelación por mora extendida |

## Planes disponibles

| Plan | Precio MVP | Características principales |
|---|---|---|
| `basic` | TBD | Catálogo, carrito, checkout, 1 pasarela de pago |
| `gold` | TBD | Básico + variantes, clientes, cupones, reportes, WhatsApp |
| `diamond` | TBD | Gold + multi-sucursal, facturación electrónica, staging |

> Los límites exactos de cada plan (cantidad de productos, usuarios, etc.) están definidos en `GOVERNANCE.md`.

## Reglas de dominio

- Solo una `Subscription` con status `Active` o `PastDue` por Tenant simultáneamente.
- Al cambiar de plan, la subscription anterior pasa a `Cancelled`; se crea una nueva.
- La activación del Tenant (`TenantStatus → Active`) ocurre cuando la Subscription pasa a `Active`.
- La suspensión del Tenant (`TenantStatus → Suspended`) ocurre cuando la Subscription pasa a `Suspended`.
- El período de gracia para `PastDue` es configurable (default: 7 días en MVP).

## Flujo de onboarding (relación con Subscription)

```
1. POST /api/onboarding/tenants
   → Tenant (PendingPayment) + Subscription (PendingActivation)
2. Buyer paga suscripción inicial
   → Webhook de pago → Subscription (Active) → Tenant (Active)
3. Cada mes: renovación automática
   → Webhook exitoso → Subscription sigue Active
   → Webhook fallido → Subscription → PastDue
4. Período de gracia (7 días): reintento de cobro
   → Exitoso → Active
   → Fallido → Subscription → Suspended → Tenant → Suspended
```

## Webhooks de billing

- El sistema de billing (Bancard / PagoPar / sistema propio) envía webhook al endpoint `/api/billing/webhooks`.
- La firma del webhook debe validarse antes de procesar.
- Idempotencia: verificar `EventId` antes de procesar (misma tabla `PaymentEventsProcessed` o tabla separada).

## Estado de implementación

✅ **Entidad implementada** — `EShopy.Domain/Subscriptions/Subscription.cs`. Se crea en
`PendingActivation` durante el onboarding y pasa a `Active` via
`POST /api/admin/tenants/{id}/activate` (SUPERADMIN), junto con el Tenant.

❌ **No implementado**: `PriceAmount` real (GOVERNANCE.md marca los 3 precios como "TBD" — se usa 0
mientras tanto, ver `EShopy.Application/Tenants/PlanPricing.cs`), integracion con Bancard/PagoPar,
webhook de pago (`POST /api/billing/webhooks`), renovacion mensual automatica, y las transiciones
`Active → PastDue` / `PastDue → Suspended` (nada las dispara todavia). Todo esto sigue planificado
para Fase 8.

Ver `workflows/onboarding-flow.md` para el flujo completo de activación de tenant.
