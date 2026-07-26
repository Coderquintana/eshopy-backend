# Domain — Tenants

> Entidades `Tenant`, `Store`, `TenantUser`: onboarding y configuración del comercio.

## Tenant — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK |
| `Subdomain` | `string` | No | Único en toda la plataforma. Ej: `"mitienda"` |
| `BusinessName` | `string` | No | Nombre legal del negocio |
| `Status` | `TenantStatus` | No | Ver tabla de estados |
| `Plan` | `TenantPlan` (enum) | No | `Basic`, `Gold`, `Diamond`. El request de onboarding acepta el string en minusculas (`"basic"`) |
| `CreatedAtUtc` | `DateTime` | No | Fecha de alta |
| `ActivatedAtUtc` | `DateTime?` | Sí | Cuando pasó a Active |

> `Tenant` es una entidad **global** (no multi-tenant). No tiene `TenantId` como FK — ella misma es el tenant.

## TenantStatus

| Estado | Descripción |
|---|---|
| `PendingPayment` | Recién creado, esperando pago de suscripción |
| `Active` | Suscripción activa. Tienda operativa |
| `Suspended` | Pago vencido. Tienda suspendida temporalmente |
| `Cancelled` | Cancelado definitivamente |

| Desde → Hacia | Trigger |
|---|---|
| PendingPayment → Active | Webhook de pago de suscripción confirmado (Fase 8, no implementado) o `POST /api/admin/tenants/{id}/activate` (SUPERADMIN, disponible hoy) |
| Active → Suspended | Falla de renovación de suscripción |
| Suspended → Active | Pago de renovación exitoso o activación manual |
| Active → Cancelled | Cancelación voluntaria |
| Suspended → Cancelled | Cancelación por mora |

## Store — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK |
| `TenantId` | `Guid` | No | FK al tenant (1:1 en MVP) |
| `Name` | `string` | No | Nombre de la tienda pública |
| `CurrencyCode` | `string` | No | `"PYG"` en MVP. Heredado por productos y pedidos |
| `Timezone` | `string` | No | `"America/Asuncion"` default |
| `PrimaryColor` | `string?` | Sí | Hex color de marca |
| `LogoUrl` | `string?` | Sí | URL del logo |
| `BackgroundColor` | `string?` | Sí | Hex color de fondo |
| `Description` | `string?` | Sí | Descripción pública |

> En MVP: 1 Store por Tenant. El `StoreId` se resuelve en backend, nunca del request.

## TenantUser — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK |
| `TenantId` | `Guid` | No | FK tenant |
| `KeycloakUserId` | `string` | No | ID del usuario en Keycloak |
| `Email` | `string` | No | Único por tenant |
| `Name` | `string` | No | Nombre visible |
| `Role` | `string` | No | `TENANT_OWNER`, `TENANT_ADMIN`, `TENANT_STAFF` |
| `IsActive` | `bool` | No | Para deshabilitar sin eliminar |

## Reglas de dominio

- `Subdomain` único en toda la plataforma — validar antes de crear Tenant.
- `CurrencyCode` del Store es la moneda para todos los productos y pedidos del tenant.
- El primer `TenantUser` creado tiene rol `TENANT_OWNER`.
- `TenantUser.Email` único por tenant — mismo email puede tener usuarios en distintos tenants.
- Un Tenant `Suspended` o `Cancelled` no puede operar (middleware debe bloquear requests).

## Índices DB

| Tabla | Índice | Tipo |
|---|---|---|
| Tenants | `Subdomain` | UNIQUE (global) |
| TenantUsers | `(TenantId, Email)` | UNIQUE |
| Stores | `TenantId` | UNIQUE (1:1 en MVP) |

## Endpoints asociados

Ver `architecture/api-contracts.md` para el detalle completo de request/response.

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/onboarding/tenants` | Público | Crear tenant (Tenant + Store + Owner en Keycloak + Subscription) |
| GET | `/api/admin/tenants/{id}` | TenantsRead (SUPERADMIN) | Detalle de tenant |
| POST | `/api/admin/tenants/{id}/activate` | TenantsWrite (SUPERADMIN) | Activacion manual — unico trigger disponible hasta que exista el webhook de pago (Fase 8) |
| GET | `/api/store` | Público | Config pública del store (Storefront) |
| PUT | `/api/store` | StoreWrite | Actualizar config del store (Admin) |
| GET | `/api/admin/users` | UsersManage | Listar usuarios (Owner+Admin+Staff) del tenant actual |
| POST | `/api/admin/users` | UsersManage | Invitar Admin o Staff al tenant actual (Owner no es invitable, se crea solo en el onboarding) |

## Estado de implementación

| Entidad | Estado |
|---|---|
| Tenant | ✅ Implementado — `EShopy.Domain/Tenants/Tenant.cs`, entidad global, maquina de estados completa |
| Store | ✅ Implementado — `EShopy.Domain/Tenants/Store.cs`, 1:1 con Tenant, `CurrencyCode` inmutable tras creacion |
| TenantUser | ✅ Implementado — Owner se crea en el onboarding, Admin/Staff via `POST /api/admin/users` (F4-05) |
| TenantResolutionMiddleware | ✅ Implementado contra DB real (`EfTenantResolver`, cache ~60s por subdominio). Bloquea `Suspended`/`Cancelled` con 403 |
