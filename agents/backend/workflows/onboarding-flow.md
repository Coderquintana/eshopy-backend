# Workflow — Onboarding Flow

> Flujo de creación y activación de un nuevo tenant (comercio) en eShopy.

> **Estado real (2026-07-26)**: el Paso 1 (creacion) y la activacion estan implementados y
> funcionando. Los Pasos 2-3 (pago via Bancard/PagoPar y su webhook) todavia no existen — Fase 8.
> Mientras tanto, la activacion es manual via `POST /api/admin/tenants/{id}/activate` (SUPERADMIN),
> una herramienta de soporte/ops que sigue siendo util incluso despues de que el webhook exista
> (casos de excepcion, regularizaciones manuales, etc.). Ver `domain/subscriptions.md`.

## Actores

| Actor | Rol |
|---|---|
| Prospect | Persona que quiere crear su tienda |
| Backend eShopy | Procesa creación de tenant y activación |
| Keycloak | Gestiona identidad del Owner |
| Provider de pago | Bancard / PagoPar — procesa suscripción |

## Flujo completo

```
Prospect                Backend                 Keycloak         Payment Provider
    │                      │                        │                    │
    │ POST /api/onboarding/tenants                   │                    │
    │ { subdomain, businessName, ownerEmail, plan }  │                    │
    │─────────────────────>│                         │                    │
    │                      │ Validar subdomain único  │                    │
    │                      │ Crear Tenant(PendingPayment)                 │
    │                      │ Crear Subscription(PendingActivation)        │
    │                      │ Crear Store(defaults)   │                    │
    │                      │────── CreateUser ──────>│                    │
    │                      │<───── KeycloakUserId ───│                    │
    │                      │ Crear TenantUser(Owner) │                    │
    │<─── 201 { tenantId, paymentUrl } ─────────────────────────────────>│
    │                      │                         │                    │
    │ Prospect accede a paymentUrl y paga             │                    │
    │                      │                         │      Webhook pago  │
    │                      │<──────────────────────────────────── POST ───│
    │                      │ Validar firma            │                    │
    │                      │ Idempotencia (EventId)  │                    │
    │                      │ Subscription → Active   │                    │
    │                      │ Tenant → Active         │                    │
    │                      │────── Enable Realm ─────>│                  │
    │                      │<──────────────────────────────────── 200 ───│
    │                      │                         │                    │
    │ Owner recibe email con credenciales (Keycloak)  │                    │
    │ Owner inicia sesión en Admin Panel              │                    │
```

## Paso 1: Solicitud de onboarding

**Endpoint**: `POST /api/onboarding/tenants` (excluido de TenantResolutionMiddleware)
**Auth**: Público

**Request:**
```json
{
  "subdomain": "mitienda",
  "businessName": "Mi Tienda SRL",
  "ownerEmail": "dueño@mitienda.com",
  "ownerName": "Juan Pérez",
  "plan": "basic"
}
```

**Validaciones:**
- `subdomain`: único en la plataforma, solo letras/números/guiones, 3-50 chars
- `ownerEmail`: formato válido
- `plan`: valor en [`basic`, `gold`, `diamond`]

**Acciones en backend (implementado):**
1. Verificar que `subdomain` no existe en Tenants
2. Crear usuario en Keycloak (realm `eshopy`, rol `TENANT_OWNER`) **antes** de escribir en la base
   local — si esto falla no queda un Tenant huerfano sin usuario
3. Crear `Tenant` (Status = `PendingPayment`), `Store` con defaults, `TenantUser` (Owner) y
   `Subscription` (Status = `PendingActivation`) en una sola transaccion
4. Retornar `{ tenantId, subdomain, status }`

**Pendiente (Fase 8):** generar URL de pago real y devolverla en la respuesta. No se inventa un
valor de `paymentUrl` mientras no exista un provider real conectado.

**Response 201 (real):**
```json
{
  "tenantId": "aaaaaaaa-...",
  "subdomain": "mitienda",
  "status": "PendingPayment"
}
```

## Paso 2: Pago de suscripción inicial

- Prospect accede a `paymentUrl` (URL de Bancard/PagoPar)
- Completa el pago en la interfaz del provider
- Provider envía webhook a `POST /api/payments/webhooks/bancard`

## Paso 3: Activación por webhook

**Endpoint**: `POST /api/payments/webhooks/{provider}`
**Auth**: Validación de firma del provider (no JWT)

```
1. Validar firma/secret del header
2. Extraer EventId del payload
3. Verificar idempotencia (PaymentEventsProcessed)
4. Buscar Subscription por referencia externa (tenantId o subscriptionId)
5. Subscription → Active (BillingCycleStart/End calculados)
6. Tenant → Active (ActivatedAtUtc = ahora)
7. Enviar email de bienvenida al Owner (opcional en MVP)
8. Guardar EventId en PaymentEventsProcessed
9. Retornar 200 OK
```

## Paso 4: Primera sesión del Owner

- Keycloak envía email de "set password" al crear el usuario
- Owner accede a Admin Panel (`admin.eshopy.com.py/mitienda`)
- Login via Keycloak (Authorization Code + PKCE)
- Primera vez: configurar Store (logo, colores, descripción)

## Estados del Tenant durante el flujo

| Paso | TenantStatus | SubscriptionStatus |
|---|---|---|
| POST /api/onboarding/tenants | `PendingPayment` | `PendingActivation` |
| Webhook pago exitoso | `Active` | `Active` |
| Falla en renovación mensual | `Active` | `PastDue` |
| Sin pago en período de gracia | `Suspended` | `Suspended` |
| Pago de regularización | `Active` | `Active` |

## Renovación mensual (ciclo de vida de suscripción)

```
Cada mes, al vencer BillingCycleEnd:
  → Provider intenta cobrar
  → Exitoso: Subscription sigue Active, BillingCycleEnd += 1 mes
  → Fallido: Subscription → PastDue
    → Período de gracia (7 días): reintento
      → Exitoso: Subscription → Active
      → Fallido: Subscription → Suspended, Tenant → Suspended
```

## Estado de implementación

| Paso | Estado |
|---|---|
| Paso 1 — Solicitud de onboarding (`POST /api/onboarding/tenants`) | ✅ Implementado |
| Paso 2 — Pago de suscripción inicial (redirect a provider) | ❌ No implementado (Fase 8) |
| Paso 3 — Activación por webhook (`POST /api/payments/webhooks/{provider}`) | ❌ No implementado (Fase 8) |
| Activación manual SUPERADMIN (`POST /api/admin/tenants/{id}/activate`) | ✅ Implementado — trigger disponible hoy |
| Paso 4 — Primera sesión del Owner (login Keycloak, configurar Store) | ✅ Desbloqueado (`PUT /api/store` implementado). El flujo de login SPA es responsabilidad del frontend |
| Renovación mensual / ciclo de suscripción | ❌ No implementado (Fase 8) |

Ver `domain/tenants.md` y `domain/subscriptions.md` para el detalle de entidades.
