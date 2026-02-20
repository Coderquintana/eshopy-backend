# GOVERNANCE — Decisiones Técnicas Firmes

> Decisiones tomadas y cerradas. No reabrir sin justificación fuerte. Fecha de última revisión: 2026-02-17.

## Decisiones de arquitectura

| Decisión | Regla | Consecuencia si se viola |
|---|---|---|
| **Result<T> oficial** | Application retorna `Result<T>`. `DomainException` solo para invariantes de dominio | Refactor completo del módulo |
| **Multi-tenancy por subdominio** | TenantId se resuelve en `TenantResolutionMiddleware` desde el host. Nunca del body del request | Vulnerabilidad de aislamiento |
| **Global Query Filters** | Todas las entidades multi-tenant tienen filtro por TenantId en EF Core | Fuga de datos entre tenants |
| **Carrito server-side** | Persistencia en BD con `CartToken`. No en `localStorage` | Inconsistencias de inventario |
| **Paginación en SQL** | `IProductRepository` recibe `PagedQuery`. Resolver en base de datos, no en memoria | Performance inaceptable en catálogos grandes |
| **OrderNumber atómico** | Generado con `TenantCounters` usando `UPDLOCK/ROWLOCK` | Duplicados en concurrencia alta |

---

## Decisiones de dominio

| Decisión | Regla |
|---|---|
| **StockOnHand obligatorio** | `int >= 0`. No nullable en dominio ni en BD. Nunca null |
| **StoreId en Products** | FK presente en `Product`. En MVP = único store del tenant (se resuelve en backend) |
| **CurrencyCode heredado** | No va en `CreateProductRequest`. Backend lo toma del Store del tenant |
| **Snapshot de precio en OrderItem** | Precio del producto al momento del checkout, no referencia dinámica |
| **Idempotencia de webhooks** | Tabla `PaymentEventsProcessed`. Evento duplicado no cambia estado |
| **Transiciones de estado cerradas** | Ver tablas en `domain/*.md`. No se permiten transiciones no listadas |

---

## Decisiones de seguridad

| Decisión | Regla |
|---|---|
| **JWT Bearer único** | Keycloak OIDC. No implementar auth propio |
| **Claim `permissions`** | Policies basadas en claim `permissions` (ej. `catalog.write`). No en roles directos |
| **Endpoints admin siempre autorizados** | Todo endpoint bajo `/api/` (no `/api/public/`) requiere `[Authorize(Policy=...)]` |
| **Buyer anónimo MVP** | Storefront anónimo en MVP. Auth de buyer es post-MVP |

---

## Rutas excluidas de TenantResolutionMiddleware

| Ruta | Motivo |
|---|---|
| `/health` | Health check sin contexto de tenant |
| `/swagger/*` | Documentación API |
| `/api/onboarding/tenants` | Crea el tenant; no puede requerir tenant existente |
| `/api/payments/webhooks/*` | Provider no envía subdominio; se resuelve por referencia interna |

---

## Convenciones de código (no negociables)

| Ámbito | Regla |
|---|---|
| **Idioma del código** | Inglés (nombres de clases, métodos, propiedades, variables) |
| **Idioma de docs/comentarios** | Español |
| **Encoding** | UTF-8 sin BOM. Nunca re-guardar con otra codificación |
| **Fechas** | ISO 8601 UTC siempre. Usar `DateTime` (UTC) o `DateTimeOffset` |
| **Columnas EF Core** | `HasComment()` obligatorio en toda columna mapeada |
| **Swagger** | `<summary>` obligatorio en todos los controllers y actions |
| **Postman** | Todo cambio en endpoints debe reflejarse en la colección Postman |
| **Commits** | `type(scope): resumen corto` — sin commits masivos sin justificación |

---

## Planes de producto (MVP vs futuros)

| Feature | Plan Básico (MVP) | Plan Gold | Plan Diamond |
|---|---|---|---|
| Tienda con subdominio | ✅ | ✅ | ✅ |
| Catálogo de productos | ✅ | ✅ | ✅ |
| Carrito y checkout | ✅ | ✅ | ✅ |
| Pagos (Bancard / PagoPar) | ✅ | ✅ | ✅ |
| Variantes de producto | ❌ | ✅ | ✅ |
| Gestión de clientes (Customer) | ❌ | ✅ | ✅ |
| Cupones y promociones | ❌ | ✅ | ✅ |
| Multi-sucursal / multi-almacén | ❌ | ❌ | ✅ |
| Facturación electrónica | ❌ | ❌ | ✅ |

---

## Roles y permisos (matriz completa)

| Política | SUPERADMIN | OWNER | ADMIN | STAFF |
|---|---|---|---|---|
| TenantsWrite | ✅ | ❌ | ❌ | ❌ |
| TenantsRead | ✅ | ❌ | ❌ | ❌ |
| StoreWrite | ✅ | ✅ | ❌ | ❌ |
| StoreRead | ✅ | ✅ | ✅ | ✅ |
| CatalogWrite | ✅ | ✅ | ✅ | ❌ |
| CatalogRead | ✅ | ✅ | ✅ | ✅ |
| OrdersWrite | ✅ | ✅ | ✅ | ❌ |
| OrdersRead | ✅ | ✅ | ✅ | ✅ |
| PaymentsRead | ✅ | ✅ | ✅ | ❌ |
| UsersManage | ✅ | ✅ | ❌ | ❌ |
| BillingManage | ✅ | ✅ | ❌ | ❌ |
