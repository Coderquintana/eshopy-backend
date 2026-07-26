# GOVERNANCE — Decisiones Técnicas Firmes

> Decisiones tomadas y cerradas. No reabrir sin justificación fuerte. Fecha de última revisión: 2026-02-17.

## Decisiones de arquitectura

| Decisión | Regla | Consecuencia si se viola |
|---|---|---|
| **Result<T> oficial** | Application retorna `Result<T>`. `DomainException` solo para invariantes de dominio | Refactor completo del módulo |
| **Multi-tenancy por subdominio** | TenantId se resuelve en `TenantResolutionMiddleware` desde el host. Nunca del body del request | Vulnerabilidad de aislamiento |
| **Excepción: TenantId sin subdominio en webhooks** | Rutas excluidas de `TenantResolutionMiddleware` que igual necesitan operar sobre datos de un tenant (ej. `/api/payments/webhooks/*`) resuelven el tenant por una referencia interna (`ProviderPaymentId`), no por host, y fijan `TenantContext.Set(tenantId)` sin subdominio (`subdomain` es opcional en la firma). Es seguro porque el Global Query Filter con `TenantId == null` es transparente (busca en todos los tenants) — no usar `IgnoreQueryFilters()` para esto | Fuga de datos si se usa `IgnoreQueryFilters()` mas alla del lookup inicial por referencia externa |
| **Global Query Filters** | Todas las entidades multi-tenant tienen filtro por TenantId en EF Core | Fuga de datos entre tenants |
| **Carrito server-side** | Persistencia en BD con `CartToken`. No en `localStorage` | Inconsistencias de inventario |
| **Paginación en SQL** | `IProductRepository` recibe `PagedQuery`. Resolver en base de datos, no en memoria | Performance inaceptable en catálogos grandes |
| **OrderNumber atómico, sin SQL crudo** | `TenantCounters` es una entidad EF normal. `CurrentValue` es `IsConcurrencyToken()` — el `UPDATE` que genera EF ya incluye `WHERE CurrentValue = @valorLeido`. Bajo contención real (verificado 2026-07-26 con checkouts concurrentes contra SQL Server), el perdedor de la carrera **no siempre** recibe `DbUpdateConcurrencyException`: a veces el `INSERT` de `Order` alcanza a ejecutarse con un `OrderNumber` ya tomado antes de que EF detecte el mismatch, y SQL Server tira una violación de índice único (`SqlException` 2601/2627) en su lugar — ver `domain/orders.md` "Bug real encontrado" para el por qué. `EfCheckoutWriter` atrapa **ambas** excepciones y reintenta el loop completo (leer contador, incrementar, guardar) unas pocas veces — capturado dentro del writer, nunca llega a `GlobalExceptionMiddleware` salvo que se agoten los intentos. `Order.AssignOrderNumber` es idempotente a propósito (no tira si se llama dos veces) porque el writer reintenta sobre la misma instancia. Se descartó a propósito la alternativa con `UPDLOCK/ROWLOCK` (SQL Server nativo): el equipo prefiere no tener SQL crudo en el proyecto, y un solo `SaveChangesAsync` con todo trackeado (`Order`+`OrderItem`s+`Payment`+`TenantCounter`) ya es atómico por si mismo | Duplicados en concurrencia alta si se pierde el reintento (o si se vuelve a atrapar solo `DbUpdateConcurrencyException`); SQL crudo colandose de nuevo si se reintroduce `UPDLOCK` sin justificación fuerte |
| **Sin Unit of Work genérico** | Cada repositorio confirma su propia transacción (`SaveChangesAsync`), igual que `EfProductRepository`. `EShopyDbContext` ya ES un Unit of Work — envolverlo en otra interfaz es abstraer una abstracción sin necesidad real. Para los flujos que sí escriben a través de varios agregados en una sola transacción (ej. onboarding: Tenant+Store+TenantUser+Subscription; checkout: Order+OrderItems+Payment+TenantCounter), se define un writer angosto de un solo propósito (`ITenantOnboardingWriter`, `ITenantActivationWriter`, `ICheckoutWriter`) en vez de un `IUnitOfWork` reutilizable. Como todo queda trackeado por EF (nada de SQL crudo), un solo `SaveChangesAsync` alcanza — no hace falta `BeginTransactionAsync` explícito en ningún writer | Reintroducir `IUnitOfWork` sin que exista una operación concreta que lo necesite (se probó y se revirtió el 2026-07-26, ver `BACKLOG.md`) |
| **Servicio externo antes que escritura local** | Cuando un caso de uso llama a un sistema externo (Keycloak, provider de pago) Y escribe localmente, la llamada externa va primero. Si falla, no se escribe nada local. El trade-off aceptado es el inverso: si la escritura local falla después de que el externo ya respondió, queda un huérfano del lado externo (recuperable manualmente), nunca un huérfano local silencioso | Huérfanos locales invisibles (ej. un Tenant sin usuario Keycloak, o un Order sin Payment real) |

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
| `/api/admin/tenants/*` | Operación a nivel plataforma (SUPERADMIN), no ligada a un subdominio comercial |
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
| **Commits** | [Conventional Commits](https://www.conventionalcommits.org/) (`type(scope): summary`). Descripción en **inglés**. Commits chicos y atómicos por funcionalidad — nunca `git add .` de decenas de archivos sin relación. Tipos usados: `feat`, `fix`, `refactor`, `docs`, `test`, `chore` |

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
