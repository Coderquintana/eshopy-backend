# eShopy — Documentación Consolidada

> **Versión:** 2.0  
> **Fecha:** 2026-02-17  
> **Propósito:** Resumen ejecutivo y guía de navegación de toda la documentación técnica de eShopy (SaaS e-commerce multi-tenant para Paraguay)

---

## 📚 Índice de Documentos

| # | Documento | Versión | Descripción |
|---|---|---|---|
| 1 | Plan Base | 2.0 | Visión, stack, arquitectura general, multi-tenancy, roles |
| 2 | Dominio y Casos de Uso | 2.0 | Entidades, estados, transiciones, UC-01..UC-10 |
| 3 | Arquitectura Backend | 2.0 | Capas, patrones, seguridad, CORS, Keycloak, Result<T>, matrices |
| 4 | Modelo de Datos | 2.0 | Tablas, columnas, índices, constraints, decisiones de diseño |
| 5 | Plan de Trabajo Backend | 2.0 | Fases, estructura de carpetas, criterios de calidad |
| 6 | Suscripción y Billing | 2.0 | Flujo de onboarding, estados, webhooks, activación |
| 7 | Testing Strategy | 2.0 | Pirámide, herramientas, casos críticos, CI |
| 8 | Arquitectura Frontend | 2.0 | Angular, theming, componentes, responsive, routing |
| 9 | Planes Gold y Diamond | 2.0 | Comparativo de planes, funcionalidades, límites |
| 10 | Contratos OpenAPI | 2.0 | DTOs, endpoints, validaciones, códigos HTTP |
| 11 | Decisiones Técnicas | 1.0 | Decisiones firmes, estado temporal, checklist producción |

---

## 🎯 Visión General (Doc 1)

**eShopy** es una plataforma SaaS de comercio electrónico tipo Shopify, orientada al mercado paraguayo. Permite que cada comercio (tenant) disponga de su propia tienda online bajo un subdominio (`tunegocio.eshopy.com.py`), gestionando productos, pedidos y pagos de forma segura y escalable.

### Objetivos
- Solución de e-commerce simple, robusta y escalable
- Onboarding rápido y self-service de comercios
- Soporte para pagos locales (Bancard, PagoPar)
- Arquitectura mantenible que soporte evolución a planes superiores

### Stack Tecnológico

| Capa | Tecnología | Versión |
|---|---|---|
| Backend | .NET / ASP.NET Core | 10 |
| ORM | Entity Framework Core | 10 |
| Base de datos | SQL Server | 2019+ |
| Identity | Keycloak (OIDC) | 24+ |
| Validación | FluentValidation | 11+ |
| Logging | Serilog | 4+ |
| Observabilidad | OpenTelemetry | 1+ |
| Documentación API | Swashbuckle | 7+ |
| Cache / Rate limit | Redis | 7+ (opcional MVP) |
| Frontend Admin | Angular | 18+ |
| Frontend Store | Angular | 18+ |
| Tests | xUnit + Testcontainers | Latest |

---

## 🏗️ Arquitectura (Doc 1, 3)

### Principios de diseño
- **Monolito modular:** un único despliegue, módulos desacoplados por dominio
- **Clean Architecture + Vertical Slices:** capas claras, casos de uso como unidades de entrega
- **Multi-tenant por subdominio:** aislamiento por TenantId en toda la capa de datos
- **Seguridad por defecto:** AuthN OIDC + AuthZ RBAC con políticas
- **Observabilidad primero:** logging estructurado con enrichers (TenantId, UserId, CorrelationId, TraceId)
- **Errores controlados:** nunca exponer stacktrace; códigos de error estables
- **Result<T> oficial:** patrón en Application. DomainException solo para invariantes de dominio

### Proyectos de la solución

| Proyecto | Responsabilidad |
|---|---|
| EShopy.Api | Host HTTP. Controllers thin, middleware pipeline, DI, Swagger |
| EShopy.Application | Casos de uso (Commands/Queries), DTOs, validadores, interfaces repos |
| EShopy.Domain | Entidades, value objects, enums, reglas invariantes, ErrorCodes, Result<T> |
| EShopy.Infrastructure | EF Core, repositorios, migraciones, integraciones externas |
| EShopy.Tests.Unit | Tests unitarios de dominio, validadores y handlers |
| EShopy.Tests.Integration | Tests de integración con DB real (Testcontainers / LocalDB) |

### Módulos (bounded contexts)

| Módulo | Responsabilidad |
|---|---|
| Tenants | Onboarding, store settings, resolución de tenant por subdominio |
| Identity | Integración Keycloak, roles/policies, UserContext |
| Catalog | Productos (CRUD), estados, publicación/archivado |
| Carts | Carrito server-side con CartToken, gestión de items |
| Orders | Checkout, creación de pedido, estados, admin de pedidos |
| Payments | Intención de pago, webhooks idempotentes, provider adapters |

---

## 🔐 Multi-Tenancy (Doc 1, 3, 4)

### Reglas no negociables
- TenantId se resuelve del subdominio/host en middleware. **Nunca del body del request.**
- Global Query Filter por TenantId en EF Core en todas las entidades multi-tenant.
- Interceptor EF impide SaveChanges si alguna entidad multi-tenant no tiene TenantId.
- Índices compuestos (TenantId, NaturalKey) en todas las tablas de negocio.

### Rutas excluidas del TenantResolutionMiddleware

| Ruta | Motivo |
|---|---|
| `/health` | Health check sin contexto de tenant |
| `/swagger/*` | Documentación API |
| `/api/onboarding/tenants` | Crea el tenant. No puede requerir tenant existente |
| `/api/payments/webhooks/*` | Provider no envía subdominio. Se resuelve por referencia |

---

## 📊 Dominio (Doc 2)

### Entidades conceptuales

| Entidad | Descripción |
|---|---|
| Tenant | Comercio cliente. Subdominio único, suscripción activa |
| Store | Tienda pública del tenant. Branding, moneda, zona horaria |
| TenantUser | Usuario admin del tenant. Referencia a Keycloak |
| Subscription | Suscripción mensual del tenant a un plan |
| Product | Artículo vendible. Precio, stock obligatorio (>= 0), estado, slug |
| Cart | Carrito anónimo con CartToken. Persistencia server-side |
| Order | Pedido desde checkout. Items con snapshot de precio |
| Payment | Transacción asociada a un pedido. Estado, provider, referencias |

### Estados y transiciones

#### ProductStatus
| Estado | Valor | Descripción |
|---|---|---|
| Draft | 0 | Recién creado. No visible en Storefront |
| Active | 1 | Publicado y visible |
| Archived | 2 | Retirado. Historial conservado |

**Transiciones válidas:**
- Draft → Active ✅
- Draft → Archived ❌
- Active → Archived ✅
- Active → Draft ❌
- Archived → Active ✅
- Archived → Draft ❌

#### OrderStatus
| Estado | Valor | Descripción |
|---|---|---|
| PendingPayment | 0 | Creado, esperando pago |
| Paid | 1 | Pago confirmado por webhook |
| Cancelled | 2 | Cancelado (pago fallido/expirado) |
| Refunded | 3 | Reembolsado |

#### PaymentStatus
| Estado | Valor | Descripción |
|---|---|---|
| Initiated | 0 | Intención creada |
| Authorized | 1 | Fondos reservados |
| Captured | 2 | Pago confirmado |
| Failed | 3 | Rechazado/fallido |
| Refunded | 4 | Reembolsado |

### Reglas base del dominio (MVP)
- Multi-tenant: TenantId obligatorio en toda operación de negocio
- Precios: snapshot en OrderItem al momento del checkout
- CurrencyCode: heredado del Store. No editable por producto
- StockOnHand: obligatorio (int >= 0). No nullable
- StoreId en Products: FK presente por extensibilidad. En MVP siempre el único Store del tenant
- Idempotencia: webhooks procesados solo una vez (PaymentEventsProcessed)
- Consistencia: Order no puede pasar a Paid sin Payment Captured confirmado

---

## 🛠️ Decisiones Técnicas Firmes (Doc 11)

| Decisión | Descripción | Fecha | Impacto |
|---|---|---|---|
| Result<T> oficial | Application retorna Result<T>. DomainException solo para invariantes | 2026-02-17 | Refactor de todos los módulos |
| StockOnHand obligatorio | int >= 0. No nullable en BD ni dominio | 2026-02-17 | Sin impacto en MVP |
| StoreId en Products | FK presente. En MVP = único store del tenant | 2026-02-17 | Agregar a Product.cs y mapping EF |
| CurrencyCode heredado | No va en CreateProductRequest. Backend lo toma del Store | 2026-02-17 | Eliminar de request |
| Paginación en query | IProductRepository recibe PagedQuery. Resolver en SQL | 2026-02-17 | Refactor de repos |
| Carrito server-side | Persistencia en BD con CartToken. No en localStorage | 2026-02-17 | Implementar en Fase 6 |
| OrderNumber atómico | TenantCounters con UPDLOCK/ROWLOCK | 2026-02-17 | Implementar en Fase 7 |

---

## 🗄️ Modelo de Datos (Doc 4)

### Columnas base (AppEntity)

Todas las tablas multi-tenant incluyen:
- `Id` (uniqueidentifier) — PK
- `TenantId` (uniqueidentifier) — FK obligatorio
- `CreatedAtUtc` (datetime2) — Fecha de creación UTC
- `CreatedBy` (nvarchar(100)) — Usuario creador
- `UpdatedAtUtc` (datetime2 NULL) — Última modificación
- `UpdatedBy` (nvarchar(100) NULL) — Usuario modificador
- `RowVersion` (rowversion) — Concurrencia optimista
- `Data` (nvarchar(max) NULL) — JSON para extensiones

### Tablas principales (MVP)

| Tabla | Descripción |
|---|---|
| Tenants | Datos del comercio. **Global (no multi-tenant)** |
| Stores | Configuración de tienda. 1 store por tenant en MVP |
| Subscriptions | Suscripción mensual del tenant |
| TenantUsers | Perfil de usuario admin. Referencia a Keycloak |
| Products | Catálogo. StoreId FK, StockOnHand obligatorio |
| ProductImages | Metadata de imágenes (URL/StorageKey) |
| Carts | Carritos con CartToken |
| CartItems | Items del carrito con snapshot de precio |
| Orders | Pedidos. OrderNumber secuencial por tenant |
| OrderItems | Items con snapshot completo |
| Payments | Transacciones. Estado, provider, referencias |
| PaymentEventsProcessed | Idempotencia de webhooks |
| AuditLogs | Auditoría app-level |

### Índices críticos

| Tabla | Índice | Tipo |
|---|---|---|
| Products | (TenantId, Slug) | UNIQUE |
| Products | (TenantId, Sku) WHERE Sku IS NOT NULL | UNIQUE filtrado |
| Orders | (TenantId, OrderNumber) | UNIQUE |
| Carts | (TenantId, CartToken) | UNIQUE |
| TenantUsers | (TenantId, Email) | UNIQUE |

---

## 🔒 Seguridad (Doc 3)

### Autenticación
- **Keycloak OIDC:** Authorization Code + PKCE para Admin
- **Storefront:** anónimo por defecto en MVP. Auth de buyer post-MVP
- **Tokens JWT:** validación de audience, issuer, firma. Tokens cortos (15 min) + refresh

### Roles y políticas

| Rol | Alcance | Descripción |
|---|---|---|
| ESHOPY_SUPERADMIN | Global | Admin global del SaaS |
| TENANT_OWNER | Por tenant | Propietario. Acceso total al tenant |
| TENANT_ADMIN | Por tenant | Admin operativo. Catálogo y pedidos |
| TENANT_STAFF | Por tenant | Permisos limitados |

### Matriz de permisos

| Política | SUPERADMIN | OWNER | ADMIN | STAFF | Módulo |
|---|---|---|---|---|---|
| TenantsWrite | ✅ | ❌ | ❌ | ❌ | Tenants |
| TenantsRead | ✅ | ❌ | ❌ | ❌ | Tenants |
| StoreWrite | ✅ | ✅ | ❌ | ❌ | Tenants |
| StoreRead | ✅ | ✅ | ✅ | ✅ | Tenants |
| CatalogWrite | ✅ | ✅ | ✅ | ❌ | Catalog |
| CatalogRead | ✅ | ✅ | ✅ | ✅ | Catalog |
| OrdersWrite | ✅ | ✅ | ✅ | ❌ | Orders |
| OrdersRead | ✅ | ✅ | ✅ | ✅ | Orders |
| PaymentsRead | ✅ | ✅ | ✅ | ❌ | Payments |
| UsersManage | ✅ | ✅ | ❌ | ❌ | Identity |
| BillingManage | ✅ | ✅ | ❌ | ❌ | Billing |

### CORS

| Ambiente | Origins | Métodos | Headers |
|---|---|---|---|
| Development | `http://localhost:4200`, `http://localhost:4201` | GET, POST, PUT, PATCH, DELETE, OPTIONS | Authorization, Content-Type, X-Correlation-Id |
| Production | `https://*.eshopy.com.py` | GET, POST, PUT, PATCH, DELETE, OPTIONS | Authorization, Content-Type, X-Correlation-Id |

---

## 📝 Logging y Auditoría (Doc 3)

### Enrichers obligatorios (Serilog)

| Enricher | Fuente | Presente en |
|---|---|---|
| TenantId | TenantContext (scoped) | Todos los requests con tenant resuelto |
| UserId | UserContext (JWT claim) | Todos los requests autenticados |
| CorrelationId | Header o generado | Todos los requests |
| TraceId | Activity.Current.TraceId | Todos los requests |
| RequestPath | HttpContext.Request.Path | Todos los requests |
| RequestMethod | HttpContext.Request.Method | Todos los requests |

### Eventos auditables (tabla AuditLog)
- Creación de Tenant y cambios de estado
- Cambios de precio de Product
- Cambios de estado de Product
- Creación de Order y cambios de estado
- Eventos de Payment / Webhook
- Cambios de roles de TenantUser

---

## 🧪 Testing (Doc 7)

### Pirámide de tests
- **Unit tests:** 70–80% (validators, dominio, handlers)
- **Integration tests:** 20–25% (EF Core, aislamiento multi-tenant, webhooks)
- **E2E:** 5–10% (post-MVP, Playwright)

### Herramientas
- xUnit + FluentAssertions + NSubstitute
- Testcontainers (SQL Server) o LocalDB
- Respawn (reset DB entre tests)
- WireMock.Net (simular providers)

### Casos críticos obligatorios
- Aislamiento multi-tenant: Product de tenantA no visible en tenantB
- Transiciones de estado: ProductStatus, OrderStatus según tablas de Doc 2
- Idempotencia webhooks: evento duplicado no cambia estado
- Concurrencia: dos PUT simultáneos generan conflicto 409
- RBAC: endpoints admin sin token retornan 401

---

## 🎨 Frontend (Doc 8)

### Aplicaciones
- **eShopy Admin:** panel del tenant (Angular)
- **eShopy Storefront:** tienda pública (Angular)

### Theming centralizado
- Tokens CSS en `:root`: colores, tipografía, spacing, radius, sombras
- Personalización por tenant: PrimaryColor, LogoUrl, BackgroundColor desde Store
- UI library usa **solo variables CSS**, nunca colores hardcodeados

### Componentes reutilizables (libs/ui)
- AppButton, AppTextField, AppSelect, AppToast, AppDialog
- AppDataGrid, AppLoading, AppPageLayout

### Responsive
- **Storefront:** mobile-first (mayoría compra desde móvil)
- **Admin:** optimizado para desktop, responsive como secundario
- Breakpoints: xs (<600px), sm (600–960px), md (960–1280px), lg (>1280px)

---

## 💳 Planes de Producto (Doc 9)

### Plan Básico (MVP)
- Tienda online con subdominio
- Catálogo de productos
- Carrito y checkout
- Pagos (Bancard / PagoPar)
- Admin panel básico
- Personalización mínima: nombre, logo, color, descripción

### Plan Gold
- Todo lo del Básico más:
- Variantes de producto (talles, colores)
- Inventario avanzado
- Gestión de clientes (Customer)
- Cupones y promociones
- Reportes operativos
- Integración WhatsApp
- Soporte prioritario

### Plan Diamond
- Todo lo de Gold más:
- Multi-sucursal / multi-almacén
- Facturación electrónica
- Integraciones contables
- Personalización avanzada del storefront
- Múltiples pasarelas de pago
- Ambiente staging/sandbox
- Soporte premium

---

## 🔄 Casos de Uso (Doc 2)

| ID | Nombre | Actores principales |
|---|---|---|
| UC-01 | Onboarding de Tenant | SuperAdmin / Prospect |
| UC-02 | Configurar Store | Tenant Owner |
| UC-03 | Gestionar usuarios del tenant | Tenant Owner |
| UC-04 | Gestionar catálogo (CRUD productos) | Tenant Admin |
| UC-05 | Gestionar carrito | Buyer |
| UC-06 | Checkout | Buyer |
| UC-07 | Iniciar pago con proveedor | Buyer / Backend |
| UC-08 | Confirmar pago (webhook) | Payment Provider / Backend |
| UC-09 | Administrar pedidos | Tenant Admin / Owner |
| UC-10 | Gestionar suscripción | Tenant Owner / Backend |

---

## 📡 Contratos API (Doc 10)

### Convenciones
- Base URL: `/api`
- Content-Type: `application/json; charset=utf-8`
- Fechas en ISO 8601 UTC
- TenantId se resuelve por host. **Nunca se envía desde frontend**

### ErrorResponse estándar
```json
{
  "traceId": "string",
  "code": "string",
  "message": "string",
  "details": {}
}
```

### Códigos de error canónicos

| Código | HTTP | Descripción |
|---|---|---|
| VALIDATION_ERROR | 400 | Error de validación de request |
| TENANT_NOT_FOUND | 404 | Tenant no encontrado para el subdominio |
| SUBDOMAIN_ALREADY_EXISTS | 409 | Subdominio ya en uso |
| UNAUTHORIZED | 401 | Token ausente o inválido |
| FORBIDDEN | 403 | Sin permisos |
| NOT_FOUND | 404 | Recurso no encontrado |
| CONFLICT | 409 | Conflicto (slug duplicado, etc) |
| PRODUCT_NOT_AVAILABLE | 409 | Producto no disponible para carrito |
| PRODUCT_INVALID_STATE | 409 | Transición de estado no permitida |
| ORDER_INVALID_STATE | 409 | Transición de orden no permitida |
| PAYMENT_WEBHOOK_INVALID | 401 | Webhook con firma inválida |
| PAYMENT_PROVIDER_ERROR | 502 | Error al comunicarse con provider |
| GENERIC_ERROR | 500 | Error interno no controlado |

### Endpoints principales

#### Catalog — Admin
- `POST /api/products` — Crear producto (Draft)
- `GET /api/products` — Listar productos (paginado)
- `GET /api/products/{id}` — Detalle producto
- `PUT /api/products/{id}` — Actualizar producto
- `PATCH /api/products/{id}/status` — Cambiar estado

#### Catalog — Storefront
- `GET /api/public/products` — Listar productos públicos (Active)
- `GET /api/public/products/{slug}` — Detalle por slug
- `GET /api/store` — Configuración pública de la tienda

#### Cart
- `POST /api/cart/items` — Agregar item
- `PUT /api/cart/items/{id}` — Actualizar cantidad
- `DELETE /api/cart/items/{id}` — Eliminar item
- `GET /api/cart` — Obtener carrito por CartToken

#### Orders
- `POST /api/checkout` — Crear Order desde carrito
- `GET /api/orders` — Listar pedidos (admin)
- `GET /api/orders/{id}` — Detalle pedido

#### Payments
- `POST /api/payments` — Iniciar pago (retorna paymentUrl)
- `POST /api/payments/webhooks/{provider}` — Webhook idempotente

#### Onboarding
- `POST /api/onboarding/tenants` — Crear tenant (PendingPayment)

---

## 🚀 Plan de Trabajo (Doc 5)

### Fase 0 — Fundaciones
- Estructura de proyectos y carpetas
- Configuración de ambientes
- Serilog + Swagger + XML docs

### Fase 1 — Contexto y Middleware
- TenantContext y resolución por subdominio
- CorrelationId + GlobalExceptionMiddleware
- BaseController.FromResult<T>
- Contrato ErrorResponse

### Fase 2 — Seguridad e Identidad
- Integración Keycloak OIDC
- UserContext + mapeo de claims
- Roles y políticas RBAC
- CORS por ambiente
- [Authorize] en todos los endpoints admin

### Fase 3 — Modelo Base y Persistencia
- AppEntity con auditoría y concurrencia
- EF Core DbContext + Global Query Filters
- Interceptor TenantId + fechas UTC
- Migración inicial con todas las tablas

### Fase 4 — Tenants (Onboarding)
- TenantEntity (no multi-tenant)
- CreateTenant con Result<T>
- Integración Keycloak para Owner
- Estados del tenant
- Tabla Subscriptions

### Fase 5 — Catálogo
- Agregar StoreId a Product.cs
- Commands con Result<T>: Create, Update, ChangeStatus
- Queries con PagedQuery: GetProducts, GetById, GetBySlug
- Validadores FluentValidation completos
- Auditoría de cambios de precio y estado
- Tabla de transiciones ProductStatus aplicada

### Fase 6 — Carrito
- CartEntity + CartItemEntity server-side
- Commands: AddCartItem, UpdateCartItem, RemoveCartItem
- Queries: GetCart por CartToken
- Job de limpieza de carritos expirados

### Fase 7 — Pedidos (Checkout)
- OrderEntity + OrderItemEntity con snapshot
- Caso de uso Checkout con Result<T>
- Generación de OrderNumber con TenantCounters
- Transiciones de OrderStatus controladas

### Fase 8 — Pagos
- PaymentEntity + IPaymentProviderAdapter
- Adaptadores: BancardAdapter, PagoParAdapter
- Endpoints webhook con idempotencia
- Validación de firma/secret

### Fase 9 — Observabilidad
- Enrichers completos: TenantId, UserId, CorrelationId, TraceId
- OpenTelemetry traces y métricas
- AuditLog en operaciones sensibles
- Matriz de logs implementada

### Fase 10 — Testing Strategy
- Tests unitarios: validadores, dominio, handlers Result<T>
- Tests integración: Testcontainers, aislamiento multi-tenant
- Tests webhooks: idempotencia, firma inválida
- Tests RBAC: políticas, endpoints sin token → 401

---

## 📦 Estado Temporal del Código (Doc 11)

### Deuda técnica conocida

| Item temporal | Estado actual | Debe ser | Fase salida |
|---|---|---|---|
| Repositorios in-memory | InMemoryProductRepository | EF Core + SQL Server | Fase 3 |
| Sin [Authorize] en admin | Todos públicos | [Authorize(Policy=...)] | Fase 2 |
| CurrencyCode hardcodeado | "PYG" literal en service | Tomado del Store | Fase 5 |
| DomainException como flujo | ProductService lanza excepciones | Result<T>.Fail(...) | Fase 5 |
| StoreId ausente en Product | Product.cs no tiene StoreId | Propiedad + Create() | Fase 5 |
| Paginación en memoria | GetAdminListAsync carga todo | PagedQuery en repo, SQL | Fase 3/5 |
| ProductService monolítico | Una clase Commands + Queries | Separar en Command/Query handlers | Fase 5 |
| localhost tenant dev | Middleware retorna 'localhost' | InMemoryTenantResolver configurable | Fase 3 |

---

## ✅ Checklist antes de Producción (Doc 11)

- [ ] Endpoints admin con [Authorize(Policy=...)]
- [ ] Repositorios in-memory → EF Core
- [ ] Result<T> en todos los handlers de Application
- [ ] StoreId en Product y resuelto en backend
- [ ] CurrencyCode tomado del Store (no hardcodeado)
- [ ] Paginación en SQL (no en memoria)
- [ ] Rutas excluidas de TenantResolutionMiddleware configuradas
- [ ] Rate limiting habilitado (públicos + pagos)
- [ ] Validación de firma webhooks implementada
- [ ] AuditLog habilitado para operaciones sensibles
- [ ] Tests unitarios + integración pasando en CI

---

## 🔗 Enlaces Útiles

- **Repositorio:** [GitHub - eShopy](https://github.com/tu-usuario/eshopy)
- **Keycloak:** [Documentación OIDC](https://www.keycloak.org/docs/latest/securing_apps/)
- **Bancard API:** [Documentación Bancard](https://www.bancard.com.py)
- **PagoPar API:** [Documentación PagoPar](https://www.pagopar.com.py)

---

## 📝 Historial de Cambios

### 2026-02-17 — v2.0 (Actualización mayor)
- Revisión completa de 11 documentos
- Agregado Doc 11: Decisiones Técnicas y Estado Temporal
- Patrón Result<T> documentado como oficial
- Matriz de roles/permisos completa
- Matriz de logs con enrichers y criticidad
- Tabla Subscriptions agregada al modelo de datos
- Decisiones sobre StockOnHand, StoreId y CurrencyCode
- Paginación en query SQL documentada
- CORS por ambiente documentado
- Rutas excluidas del TenantResolutionMiddleware
- Transiciones de estados documentadas con tablas

### 2026-02-07
- Consolidación inicial de documentación
- Colección Postman base para MVP

### 2026-02-05
- Inicio del proyecto eShopy
- Documentación base (Docs 1-10 v1.0)

---

**Mantenido por:** Equipo eShopy  
**Última actualización:** 2026-02-17