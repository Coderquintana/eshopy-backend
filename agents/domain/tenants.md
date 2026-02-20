# Domain — Tenants

> Entidades `Tenant`, `Store`, `TenantUser`: onboarding y configuración del comercio.

## Tenant — Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `Guid` | No | PK |
| `Subdomain` | `string` | No | Único en toda la plataforma. Ej: `"mitienda"` |
| `BusinessName` | `string` | No | Nombre legal del negocio |
| `Status` | `TenantStatus` | No | Ver tabla de estados |
| `Plan` | `string` | No | `"basic"`, `"gold"`, `"diamond"` |
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
| PendingPayment → Active | Webhook de pago de suscripción confirmado |
| Active → Suspended | Falla de renovación de suscripción |
| Suspended → Active | Pago de renovación exitoso |
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

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/onboarding/tenants` | Público | Crear tenant (inicia onboarding) |
| GET | `/api/store` | Público | Config pública del store (Storefront) |
| PUT | `/api/store` | StoreWrite | Actualizar config del store (Admin) |
| GET | `/api/admin/users` | UsersManage | Listar usuarios del tenant |
| POST | `/api/admin/users` | UsersManage | Invitar usuario al tenant |

## Estado de implementación

| Entidad | Estado |
|---|---|
| Tenant | ❌ No implementado (Fase 4) |
| Store | ⚠️ Skeleton — `StoreController` retorna datos hardcodeados |
| TenantUser | ❌ No implementado (Fase 4) |
| TenantResolutionMiddleware | ✅ Implementado (InMemoryTenantResolver — placeholder) |
