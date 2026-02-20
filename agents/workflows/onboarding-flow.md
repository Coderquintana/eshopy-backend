# Workflow — Onboarding Flow

> Flujo de creación y activación de un nuevo tenant (comercio) en eShopy.

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

**Acciones en backend:**
1. Verificar que `subdomain` no existe en Tenants
2. Crear `Tenant` (Status = `PendingPayment`)
3. Crear `Subscription` (Status = `PendingActivation`, plan, precio actual)
4. Crear `Store` con valores default (Name = businessName, CurrencyCode = "PYG", Timezone = "America/Asuncion")
5. Crear usuario en Keycloak (realm `eshopy`, rol `TENANT_OWNER`)
6. Crear `TenantUser` (KeycloakUserId, Email, Role = TENANT_OWNER)
7. Generar URL de pago de suscripción inicial
8. Retornar `{ tenantId, paymentUrl }`

**Response 201:**
```json
{
  "tenantId": "aaaaaaaa-...",
  "paymentUrl": "https://pagos.bancard.com.py/..."
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

❌ **No implementado.** Planificado en Fase 4 del backlog.

Ver `domain/tenants.md` y `domain/subscriptions.md` para el detalle de entidades.
