# Documentation

Compilado automaticamente desde la carpeta `Documentation Copy`.

## eShopy_Documento_10_Contratos_OpenAPI_v1.0

eShopy – Documento 10: Contratos OpenAPI (Requests/Responses reales – MVP)
Versión: 1.0
Propósito: definir los contratos HTTP (requests/responses) del MVP de eShopy con un enfoque compatible con OpenAPI. Los contratos están en inglés (nombres de campos y modelos). El backend gobierna validaciones; el frontend consume estos contratos.
1. Convenciones de API
- Base URL: /api
- Content-Type: application/json; charset=utf-8
- Todos los recursos admin requieren Bearer JWT (Keycloak) salvo endpoints públicos.
- Multi-tenant: Tenant se resuelve por host/subdomain en backend. Nunca se envía TenantId desde el frontend.
- Fechas en ISO 8601 UTC (ej. 2026-02-05T19:00:00Z).
- Id: UUID (string, format uuid).
1.1 Headers estándar
- X-Correlation-Id: opcional (si no se envía, el backend genera uno).
- Authorization: Bearer <jwt> (admin).
- Host/Subdomain: usado para resolver tenant (storefront y admin).
2. Modelo de respuesta de error (estándar)
ErrorResponse
{  "traceId": "string",  "code": "string",  "message": "string",  "details": { }}
2.1 Errores comunes (code)
- VALIDATION_ERROR
- TENANT_NOT_FOUND
- SUBDOMAIN_ALREADY_EXISTS
- UNAUTHORIZED
- FORBIDDEN
- NOT_FOUND
- CONFLICT
- PAYMENT_WEBHOOK_INVALID
- PAYMENT_PROVIDER_ERROR
- GENERIC_ERROR
3. Paginación (estándar)
Query params: page (>=1), pageSize (1..100), sort (field), dir (asc|desc)
PagedResult<T>
{  "items": [ ... ],  "page": 1,  "pageSize": 10,  "totalCount": 123}
4. Schemas (DTOs) – MVP
Nota: Son contratos de transporte. No son Entities de dominio.
4.1 Tenants / Stores
CreateTenantRequest
{  "subdomain": "string",  "legalName": "string",  "displayName": "string",  "ownerEmail": "string"}
CreateTenantResponse
{  "tenantId": "uuid",  "status": "PendingPayment",  "adminUrl": "string"}
StorePublicDto
{  "storeId": "uuid",  "name": "string",  "currencyCode": "PYG",  "timezone": "America/Asuncion",  "primaryColor": "string|null",  "logoUrl": "string|null"}
4.2 Catalog
ProductStatus: Draft | Active | Archived
ProductAdminDto
{  "id": "uuid",  "slug": "string",  "sku": "string|null",  "name": "string",  "description": "string|null",  "price": 0.0,  "currencyCode": "PYG",  "status": "Draft",  "stockOnHand": 0,  "createdAtUtc": "2026-02-05T19:00:00Z",  "updatedAtUtc": "2026-02-05T19:00:00Z"}
CreateProductRequest
{  "slug": "string",  "sku": "string|null",  "name": "string",  "description": "string|null",  "price": 0.0,  "stockOnHand": 0}
UpdateProductRequest
{  "name": "string",  "description": "string|null",  "price": 0.0,  "stockOnHand": 0,  "sku": "string|null"}
ChangeProductStatusRequest
{  "status": "Active"}
ProductPublicDto
{  "id": "uuid",  "slug": "string",  "name": "string",  "description": "string|null",  "price": 0.0,  "currencyCode": "PYG"}
4.3 Cart
CartDto
{  "cartId": "uuid",  "cartToken": "string",  "items": [    {      "id": "uuid",      "productId": "uuid",      "name": "string",      "quantity": 1,      "unitPrice": 0.0,      "lineTotal": 0.0    }  ],  "subtotal": 0.0,  "total": 0.0,  "currencyCode": "PYG"}
AddCartItemRequest
{  "cartToken": "string",  "productId": "uuid",  "quantity": 1}
UpdateCartItemRequest
{  "quantity": 1}
4.4 Orders / Checkout
OrderStatus: PendingPayment | Paid | Cancelled | Refunded
CheckoutRequest
{  "cartToken": "string",  "buyer": {    "name": "string",    "email": "string",    "phone": "string|null",    "document": "string|null"  },  "shippingAddress": "string|null"}
CheckoutResponse
{  "orderId": "uuid",  "orderNumber": 1001,  "status": "PendingPayment",  "total": 0.0,  "currencyCode": "PYG"}
OrderAdminDto (summary)
{  "id": "uuid",  "orderNumber": 1001,  "status": "PendingPayment",  "buyerEmail": "string",  "total": 0.0,  "createdAtUtc": "2026-02-05T19:00:00Z"}
4.5 Payments
PaymentProvider: Bancard | PagoPar
PaymentStatus: Initiated | Authorized | Captured | Failed | Refunded
CreatePaymentRequest
{  "orderId": "uuid",  "provider": "Bancard"}
CreatePaymentResponse
{  "paymentId": "uuid",  "status": "Initiated",  "paymentUrl": "string"}
PaymentWebhookEvent (normalized)
{  "provider": "Bancard",  "externalEventId": "string",  "externalPaymentId": "string|null",  "externalReference": "string|null",  "result": "Captured|Failed",  "amount": 0.0,  "currencyCode": "PYG",  "occurredAtUtc": "2026-02-05T19:00:00Z"}
5. Endpoints (MVP) – Contratos y códigos HTTP
GET /api/store
Auth: Public
Descripción: Obtiene configuración pública de la tienda resuelta por host/subdominio.
Response: StorePublicDto
HTTP Codes:
- 200 OK
- 404 TENANT_NOT_FOUND -> ErrorResponse
GET /api/public/products
Auth: Public
Descripción: Lista productos públicos (status=Active) con paginación.
Response: PagedResult<ProductPublicDto>
HTTP Codes:
- 200 OK
- 404 TENANT_NOT_FOUND -> ErrorResponse
GET /api/public/products/{slug}
Auth: Public
Descripción: Obtiene detalle público por slug.
Response: ProductPublicDto
HTTP Codes:
- 200 OK
- 404 NOT_FOUND -> ErrorResponse
- 404 TENANT_NOT_FOUND -> ErrorResponse
POST /api/products
Auth: Admin (CatalogWrite)
Descripción: Crea un producto (Draft por defecto).
Request: CreateProductRequest
Response: ProductAdminDto
HTTP Codes:
- 201 Created
- 400 VALIDATION_ERROR
- 409 CONFLICT (slug/sku)
- 401/403
GET /api/products
Auth: Admin
Descripción: Lista productos (admin) con filtros opcionales.
Response: PagedResult<ProductAdminDto>
HTTP Codes:
- 200 OK
- 401/403
GET /api/products/{id}
Auth: Admin
Descripción: Obtiene detalle admin.
Response: ProductAdminDto
HTTP Codes:
- 200 OK
- 404 NOT_FOUND
- 401/403
PUT /api/products/{id}
Auth: Admin (CatalogWrite)
Descripción: Actualiza campos editables.
Request: UpdateProductRequest
Response: ProductAdminDto
HTTP Codes:
- 200 OK
- 400 VALIDATION_ERROR
- 404 NOT_FOUND
- 409 CONFLICT (concurrency)
- 401/403
PATCH /api/products/{id}/status
Auth: Admin (CatalogWrite)
Descripción: Cambia estado del producto (Publish/Archive).
Request: ChangeProductStatusRequest
Response: ProductAdminDto
HTTP Codes:
- 200 OK
- 400 VALIDATION_ERROR
- 404 NOT_FOUND
- 409 PRODUCT_INVALID_STATE/CONFLICT
- 401/403
POST /api/cart/items
Auth: Public
Descripción: Agrega item al carrito (identificado por cartToken).
Request: AddCartItemRequest
Response: CartDto
HTTP Codes:
- 200 OK
- 400 VALIDATION_ERROR
- 409 PRODUCT_NOT_AVAILABLE
- 404 TENANT_NOT_FOUND
PUT /api/cart/items/{id}
Auth: Public
Descripción: Actualiza cantidad del item.
Request: UpdateCartItemRequest
Response: CartDto
HTTP Codes:
- 200 OK
- 400 VALIDATION_ERROR
- 404 NOT_FOUND
- 409 PRODUCT_NOT_AVAILABLE
DELETE /api/cart/items/{id}
Auth: Public
Descripción: Elimina item del carrito.
Response: CartDto
HTTP Codes:
- 200 OK
- 404 NOT_FOUND
GET /api/cart
Auth: Public
Descripción: Obtiene carrito por cartToken (query param).
Response: CartDto
HTTP Codes:
- 200 OK
- 400 VALIDATION_ERROR
POST /api/checkout
Auth: Public
Descripción: Crea un Order PendingPayment desde el carrito.
Request: CheckoutRequest
Response: CheckoutResponse
HTTP Codes:
- 201 Created
- 400 VALIDATION_ERROR
- 409 CONFLICT (price changed/invalid cart)
GET /api/orders
Auth: Admin (OrdersRead)
Descripción: Lista pedidos (admin).
Response: PagedResult<OrderAdminDto>
HTTP Codes:
- 200 OK
- 401/403
GET /api/orders/{id}
Auth: Admin (OrdersRead)
Descripción: Detalle de pedido (admin).
Response: OrderAdminDto (extendido)
HTTP Codes:
- 200 OK
- 404 NOT_FOUND
- 401/403
POST /api/payments
Auth: Public
Descripción: Inicia un pago para un order (retorna paymentUrl).
Request: CreatePaymentRequest
Response: CreatePaymentResponse
HTTP Codes:
- 201 Created
- 400 VALIDATION_ERROR
- 404 NOT_FOUND (order)
- 409 ORDER_INVALID_STATE
- 502 PAYMENT_PROVIDER_ERROR
POST /api/payments/webhooks/{provider}
Auth: Public (provider-to-server)
Descripción: Webhook/callback del provider. Idempotente.
Request: Provider-specific payload (validated)
Response: 204 No Content
HTTP Codes:
- 204 No Content
- 401/403 PAYMENT_WEBHOOK_INVALID
- 200 OK (duplicate event)
- 404 NOT_FOUND (payment)
POST /api/onboarding/tenants
Auth: Public (Google-auth) / or SuperAdmin
Descripción: Crea tenant en PendingPayment e inicia suscripción.
Request: CreateTenantRequest
Response: CreateTenantResponse
HTTP Codes:
- 201 Created
- 400 VALIDATION_ERROR
- 409 SUBDOMAIN_ALREADY_EXISTS
6. Idempotencia y reintentos (normas)
- Webhooks de pagos: idempotencia obligatoria por (provider, externalEventId).
- Endpoints de creación sensibles pueden aceptar Idempotency-Key (header) en el futuro (post-MVP).
- GET puede reintentar (retry) en frontend; POST/PUT/PATCH no se reintentan sin idempotencia.
7. Seguridad de contratos
- No exponer stacktraces ni mensajes internos en message.
- No retornar datos sensibles del provider (token/keys). Guardar y truncar payloads.
- Validar content-type, tamaños máximos y rate limit en endpoints públicos y webhooks.
Nota de consistencia: Sku es opcional y solo se valida/usa cuando esta presente.

## eShopy_Documento_1_Plan_Base_v1.1.docx

eShopy – SaaS E-Commerce Platform (Plan Básico)
1. Visión General
Este proyecto tiene como objetivo desarrollar una plataforma SaaS de tienda online orientada al mercado paraguayo, pensada inicialmente para pequeños y medianos comercios. El sistema permitirá a cada cliente (tenant) gestionar su propia tienda, productos y ventas de forma segura y escalable.
2. Objetivos
- Proveer una solución de e-commerce simple y robusta.- Permitir onboarding rápido de comercios.- Soportar pagos locales.- Diseñar una arquitectura escalable y mantenible.
3. Alcance del Plan Básico (MVP)
Multi-tenant por subdominio/host; aislamiento por TenantId en datos.
4. Fuera de Alcance (Plan Básico)
- Inventario avanzado.- Multi-sucursal.- Facturación electrónica.- Integraciones contables.- SSO corporativo (Azure AD).
5. Arquitectura General
El sistema se implementará como un monolito modular utilizando .NET 10 en el backend y Angular en el frontend. La autenticación se realizará mediante Keycloak usando OIDC.
6. Stack Tecnológico
- Backend: .NET 10 / ASP.NET Core- Frontend: Angular- Base de datos: SQL Server- Autenticación: Keycloak (OIDC)- ORM: Entity Framework Core- Logging: Serilog- Observabilidad: OpenTelemetry
7. Multi-Tenancy
El modelo multi-tenant se implementará inicialmente mediante una columna TenantId en todas las tablas de negocio. Esto permite simplicidad operativa y escalabilidad inicial.
8. Seguridad
- Autenticación centralizada con Keycloak.- Autorización basada en roles (RBAC).- Roles por tenant y roles globales.- Tokens JWT con expiración corta.
9. Manejo de Errores y Logs
El backend contará con manejo global de excepciones, retornando respuestas controladas y mensajes estandarizados. Los logs serán estructurados e incluirán TenantId y CorrelationId.
10. Identidad del Sistema
eShopy es una plataforma SaaS de comercio electrónico tipo Shopify, orientada inicialmente al mercado paraguayo. Permite que cada comercio (tenant) disponga de su propia tienda online bajo un subdominio, gestionando productos, pedidos y pagos de forma segura y escalable.
11. Modelo Conceptual Inicial
Conceptos principales del dominio:- Tenant: Comercio cliente de eShopy.- Store: Tienda online asociada a un tenant.- Product: Artículo ofrecido en la tienda.- Customer: Comprador final.- Order: Pedido generado desde el carrito.- Payment: Transacción asociada a un pedido.
12. Reglas Base de Multi-Tenancy
- Todo acceso a datos debe estar estrictamente filtrado por TenantId.- El TenantId se obtiene del subdominio y del contexto de autenticación.- No se aceptan valores de TenantId enviados desde el frontend.- Se definirán índices compuestos que incluyan TenantId.
13. Roles Iniciales del Sistema
- ESHOPY_SUPERADMIN: Administración global del SaaS.- TENANT_OWNER: Propietario del comercio.- TENANT_ADMIN: Administrador operativo.- TENANT_STAFF: Usuario con permisos limitados.

## eShopy_Documento_2_Dominio_y_Casos_de_Uso_v1.0

eShopy – Documento 2: Dominio y Casos de Uso (Plan Básico)
Versión: 1.0
Propósito: definir el dominio (conceptos y reglas) y los casos de uso del Plan Básico, sirviendo de base para el diseño de APIs y la arquitectura del backend.
1. Alcance
Este documento cubre el dominio funcional mínimo (MVP) de eShopy y los casos de uso principales para: creación/gestión de tienda (tenant), catálogo, carrito, checkout, pago y pedidos.
2. Actores
- Buyer (Comprador): usuario final que navega, arma carrito y compra.
- Tenant Owner: propietario del comercio; configura tienda y administra todo.
- Tenant Admin: administra catálogo y pedidos.
- Tenant Staff: usuario operativo con permisos limitados (p. ej. preparación de pedidos).
- eShopy SuperAdmin (interno): administración global del SaaS (soporte, monitoreo, tenants).
- Payment Provider (externo): pasarela (ej. Bancard/PagoPar) que procesa pagos.
3. Dominio (conceptos)
3.1 Entidades conceptuales (no es modelo físico)
- Tenant: Comercio cliente de eShopy. Tiene configuración propia y un subdominio.
- Store: Representa la tienda pública de un tenant (branding, políticas, medios de contacto).
- User: Usuario de administración asociado a un tenant, con roles RBAC.
- Product: Artículo vendible. En MVP: precio, descripción, imágenes, estado, stock simple opcional.
- Cart: Carrito del comprador. Puede ser anónimo (cookie/session) o asociado a email/usuario comprador.
- Order: Pedido resultante del checkout. Tiene items, totales, estado y referencias de pago.
- Payment: Transacción. Guarda estado, provider, identificadores externos y auditoría de cambios.
3.2 Estados mínimos
- ProductStatus: Draft | Active | Archived
- OrderStatus: PendingPayment | Paid | Cancelled | Refunded
- PaymentStatus: Initiated | Authorized | Captured | Failed | Refunded
3.3 Reglas base (MVP)
- Multi-tenant: todo dato de negocio debe pertenecer a un TenantId; el TenantId se resuelve del subdominio/contexto, nunca del body.
- Precios: el precio vigente al momento del checkout se copia al OrderItem (no depender del Product para auditoría).
- Idempotencia de pago: callbacks/webhooks deben ser idempotentes (mismo evento no debe duplicar estado ni pedidos).
- Consistencia: un Order no puede pasar a Paid si no existe Payment confirmado por el provider (o equivalente).
- Auditoría mínima: cambios de precio, estado de producto, cambios de estado del pedido y del pago deben quedar registrados (app-level logging/audit).
4. Casos de uso (MVP)
Los casos de uso se identifican como UC-xx. Se describen flujos principales y reglas relevantes.
ID
Nombre
Actores
UC-01
Onboarding de Tenant (crear tienda)
Tenant Owner / eShopy SuperAdmin
UC-02
Gestionar catálogo (crear/editar producto)
Tenant Owner/Admin
UC-03
Publicar/archivar producto
Tenant Owner/Admin
UC-04
Navegar tienda y ver producto
Buyer
UC-05
Gestionar carrito (agregar/quitar/actualizar)
Buyer
UC-06
Checkout (capturar datos comprador + envío)
Buyer
UC-07
Iniciar pago con proveedor
Buyer + Payment Provider
UC-08
Confirmar pago (callback/webhook) y marcar pedido
Payment Provider + Backend
UC-09
Administrar pedidos (listar, ver detalle, cambiar estado operativo)
Tenant Staff/Admin
UC-10
Gestión de usuarios del tenant (RBAC básico)
Tenant Owner
5. Detalle de casos de uso clave
UC-01 – Onboarding de Tenant (crear tienda)
Objetivo: Registrar un nuevo tenant y su tienda, creando el subdominio y el usuario Owner.
Precondiciones:
- El sistema eShopy está operativo.
- El subdominio elegido no existe.
Flujo principal:
1. SuperAdmin o usuario de onboarding inicia alta de tenant.
2. Se registran datos básicos: nombre comercial, RUC/CI (opcional MVP), email Owner, subdominio.
3. Se valida unicidad del subdominio y email.
4. Se crea Tenant + Store (config mínima).
5. Se crea usuario Owner en Keycloak y se asigna rol TENANT_OWNER.
6. Se envía invitación de acceso (email/whatsapp) con link al Admin.
Flujos alternativos / errores:
- Si el subdominio ya existe: rechazar con error controlado.
- Si falla Keycloak: rollback lógico (tenant queda en estado PendingSetup) y se reintenta desde panel.
Postcondiciones:
- Tenant y Store quedan creados.
- Owner puede acceder al Admin del tenant.
UC-05 – Gestionar carrito
Objetivo: Permitir al comprador armar un carrito consistente y calculable.
Precondiciones:
- El buyer está navegando una tienda válida (tenant resuelto).
Flujo principal:
1. Buyer agrega un producto al carrito.
2. El sistema valida que el producto esté Active y disponible (si hay stock simple).
3. El carrito guarda item con snapshot de precio actual (para mostrar) y cantidad.
4. Buyer puede aumentar/disminuir cantidad y eliminar items.
5. El sistema recalcula subtotal/total con reglas simples (MVP sin cupones).
Flujos alternativos / errores:
- Si el producto está Archived o no disponible: impedir agregar y mostrar mensaje.
- Si la cantidad supera stock (si aplica): ajustar al máximo permitido.
Postcondiciones:
- El carrito queda persistido (cookie/session o storage) y listo para checkout.
UC-06 – Checkout (capturar datos comprador + envío)
Objetivo: Transformar el carrito en un pedido PendingPayment con datos del comprador.
Precondiciones:
- Existe un carrito con al menos 1 item.
Flujo principal:
1. Buyer inicia checkout.
2. Se capturan datos: nombre, documento (opcional), email, teléfono, dirección (si aplica delivery).
3. El sistema valida campos mínimos y normaliza formato (teléfono, email).
4. Se crea Order en estado PendingPayment con items (precio copiado a OrderItem).
5. Se retorna resumen del pedido e instrucciones para pago.
Flujos alternativos / errores:
- Si el carrito está vacío: bloquear y redirigir a la tienda.
- Si hay cambios de precio entre carrito y checkout: el sistema recalcula y notifica antes de confirmar.
Postcondiciones:
- Se crea Order PendingPayment con totales y trazabilidad.
UC-07 – Iniciar pago con proveedor
Objetivo: Crear una intención de pago y redirigir al comprador (o abrir modal) para pagar.
Precondiciones:
- Order existe y está PendingPayment.
Flujo principal:
1. Backend crea Payment Initiated para el Order (provider seleccionado).
2. Backend solicita al provider una referencia/checkout session.
3. Se obtiene URL/token de pago del provider.
4. Frontend redirige o muestra pantalla de pago.
Flujos alternativos / errores:
- Si provider no responde: Payment Failed y Order permanece PendingPayment con opción de reintentar.
Postcondiciones:
- Payment queda creado con referencias externas.
UC-08 – Confirmar pago (callback/webhook) y marcar pedido
Objetivo: Actualizar estado del pago y del pedido de forma idempotente al recibir confirmación del provider.
Precondiciones:
- Provider envía callback/webhook firmado/validado.
- Payment existe.
Flujo principal:
1. Backend valida autenticidad (firma/secret) del webhook.
2. Backend identifica Payment/Order por referencia externa.
3. Se aplica idempotencia: si el evento ya fue procesado, se retorna OK sin cambios.
4. Si pago confirmado: Payment -> Captured (o equivalente) y Order -> Paid.
5. Se registra auditoría (logs/eventos) con payload mínimo y traceId.
Flujos alternativos / errores:
- Webhook inválido: rechazar y registrar evento de seguridad.
- Pago fallido: Payment -> Failed; Order permanece PendingPayment o Cancelled según regla del provider.
Postcondiciones:
- Order queda Paid y visible en Admin para preparación.
6. Diagramas (Mermaid)
Los siguientes diagramas están en formato Mermaid para pegar en Notion/Git/Docs. En siguientes versiones se pueden convertir a imágenes e incrustar en Word.
6.1 Diagrama de Contexto
```mermaidflowchart LR  Buyer[Buyer] -->|Navega / Compra| Storefront[Storefront (Angular)]  TenantAdmin[Tenant Admin/Owner] -->|Gestiona| AdminUI[Admin UI (Angular)]  Storefront -->|API| Backend[Backend .NET 10]  AdminUI -->|API| Backend  Backend -->|Auth OIDC| Keycloak[Keycloak]  Backend -->|Pagos| PaymentProvider[Payment Provider]  Backend -->|SQL| SqlServer[(SQL Server)]  PaymentProvider -->|Webhook/Callback| Backend```
6.2 Secuencia – Checkout + Pago
```mermaidsequenceDiagram  participant B as Buyer  participant S as Storefront  participant A as API (.NET)  participant P as Payment Provider  B->>S: Confirmar carrito  S->>A: POST /checkout (cart + buyerData)  A-->>S: OrderId (PendingPayment)  S->>A: POST /payments (OrderId)  A->>P: Crear sesión de pago  P-->>A: paymentUrl/reference  A-->>S: paymentUrl  S-->>B: Redirigir a pago  P-->>A: Webhook pago confirmado  A-->>P: 200 OK (idempotente)  A-->>S: Order Paid (polling/websocket futuro)```
7. Entregables siguientes
- Documento 3: Arquitectura Backend (módulos, capas, contratos, multi-tenant enforcement).- Definición de APIs (OpenAPI) para UC-01..UC-10.- Modelo ER inicial y naming conventions.

## eShopy_Documento_3_Arquitectura_Backend_v1.2

eShopy – Documento 3: Arquitectura Backend (Plan Básico)
Versión: 1.0
Propósito: definir la arquitectura backend para el Plan Básico de eShopy (MVP), incluyendo módulos, capas, convenciones, multi-tenancy, seguridad, errores, logging y auditoría.
1. Principios de diseño
- Monolito modular: un único despliegue, módulos desacoplados por frontera de dominio.
- Clean Architecture + Vertical Slices: capas claras y casos de uso como unidades de entrega.
Multi-tenant por subdominio/host; aislamiento por TenantId obligatorio en toda la capa de datos.
- Seguridad por defecto: AuthN OIDC (Keycloak) + AuthZ RBAC y políticas.
- Observabilidad primero: logging estructurado + correlation/trace id en todas las requests.
- Errores controlados: nunca exponer stacktrace al cliente; códigos de error estables.
2. Stack backend
- .NET 10 / ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- Keycloak (OIDC)
- Serilog (JSON)
- OpenTelemetry (traces/metrics)
- (Opcional MVP) Redis para cache y rate limiting distribuido
3. Estructura de solución (propuesta)
Convención: nombres en inglés para proyectos/código/tablas. El branding es eShopy, pero el código puede usar un nombre técnico (ej. EShopy o EShopyCore).
Solución (ejemplo):- EShopy.Api (Host)- EShopy.Shared (cross-cutting: primitives, results, errors)- EShopy.Identity (authn/authz adapters, policies)- EShopy.Tenants (onboarding, store settings)- EShopy.Catalog (products)- EShopy.Carts (cart)- EShopy.Orders (orders)- EShopy.Payments (provider integrations)- EShopy.Infrastructure (EF, migrations, logging, outbox opcional)
4. Módulos (bounded contexts) – MVP
- Tenants: Alta de tenant/store, configuración mínima, resolución de tenant por subdominio.
- Identity: Integración Keycloak, roles/policies, extracción de claims y contexto de usuario.
- Catalog: Productos (CRUD), publicación/archivado, imágenes (MVP: metadata).
- Carts: Carrito (agregar/quitar/actualizar). Persistencia: session/cookie o storage server-side (definir).
- Orders: Checkout, creación de pedido, estados, administración de pedidos.
- Payments: Creación de intención de pago, callbacks/webhooks, idempotencia, estado de pagos.
5. Capas y responsabilidades
5.1 API/Host (EShopy.Api)
- Controllers minimalistas o Endpoints (Minimal APIs) por feature.
- Validación de request (FluentValidation) y normalización (pipes/filters).
- Enriquecimiento de contexto: TenantContext + UserContext + CorrelationId.
5.2 Application (Use Cases)
- Casos de uso: comandos/queries (UC-xx).
- Orquestación de repositorios/servicios de dominio.
- Result<T> consistente con códigos de error estables.
5.3 Domain
- Entidades/agregados, value objects, reglas invariantes.
- Eventos de dominio opcionales para auditoría/outbox (post-MVP).
5.4 Infrastructure
- EF Core, Migrations, Repositories.
- Integraciones externas (Payment Provider, Email/WhatsApp post-MVP).
6. Multi-tenancy (enforcement)
Regla no negociable: el TenantId se resuelve del subdominio y/o token, y se propaga por un TenantContext. Nunca se acepta TenantId desde el body.
6.1 Resolución de tenant
- Middleware extrae host/subdominio.- Resuelve TenantId consultando tabla Tenants (cacheable).- Setea TenantContext (TenantId, StoreId, Subdomain).
6.2 Filtrado en EF Core
- Global Query Filter por TenantId en entidades multi-tenant.- Interceptor que impide SaveChanges si alguna entidad multi-tenant no tiene TenantId.- Índices compuestos (TenantId, NaturalKey).
6.3 Excepciones
- Tablas globales (ej. Tenants, AuditLog global, Providers) pueden ser multi-tenant opcional o global.
7. Seguridad (Keycloak + RBAC)
7.1 Autenticación
- OIDC Authorization Code + PKCE (front) -> API valida JWT.
- Scopes mínimos: openid, profile, email.
- Tokens cortos + refresh token (en front) según política.
7.2 Autorización
- Roles (claims): ESHOPY_SUPERADMIN, TENANT_OWNER, TENANT_ADMIN, TENANT_STAFF.- Policies por módulo: CatalogWrite, OrdersRead, OrdersWrite, UsersManage.- En endpoints: [Authorize(Policy=...)] y/o requirement handlers.
7.3 Hardening mínimo
- CORS restringido por ambientes.- Rate limiting (por IP + por tenant) – al menos para auth y pagos.- Validación de webhook: firma/secret + allowlist IP si el provider lo permite.- Headers de seguridad: HSTS, X-Content-Type-Options, etc. (config en API gateway o app).
8. Manejo de errores (backend de hierro)
Se define una respuesta de error estable para el frontend, evitando 'Ha ocurrido un error' sin contexto. El frontend puede mapear por 'code' para mensajes amigables.
8.1 Contrato de error sugerido
{  "traceId": "00-..."  ,"code": "TENANT_NOT_FOUND"  ,"message": "Tenant not found."  ,"details": null}
8.2 Global Exception Handler
- Middleware captura excepciones no controladas.- Registra log con traceId y tenant.- Retorna 500 con code GENERIC_ERROR.- Validaciones retornan 400 con code VALIDATION_ERROR y lista de fields.
8.3 Errores de dominio (ejemplos)
- TENANT_NOT_FOUND
- SUBDOMAIN_ALREADY_EXISTS
- PRODUCT_NOT_AVAILABLE
- ORDER_INVALID_STATE
- PAYMENT_PROVIDER_ERROR
- PAYMENT_WEBHOOK_INVALID
9. Logging y auditoría
9.1 Logging
- Serilog JSON con enrichers: TenantId, UserId, CorrelationId, TraceId, RequestPath.- Niveles: Information (negocio), Warning (anomalías), Error (fallos), Critical (infra).- No loguear datos sensibles (tarjetas, tokens completos).
9.2 Auditoría (recomendada a nivel aplicación)
- Tabla AuditLog con: TenantId, Entity, EntityId, Action, Before/After (json), UserId, Timestamp.- Interceptor EF o SaveChanges pipeline para generar audit en operaciones sensibles:  • cambios de precio  • cambios de estado de producto  • cambios de estado de pedido  • eventos de pago/webhook- Triggers SQL: solo si se requiere auditoría forense o protección ante accesos fuera de la app.
10. Persistencia y modelos base
10.1 AppEntity (base)
Se recomienda un modelo base que standardice:- Id (GUID o bigint)- TenantId (obligatorio en entidades multi-tenant)- CreatedAt, CreatedBy- UpdatedAt, UpdatedBy- RowVersion (concurrency)- Data (json) para extensiones no críticas
10.2 Concurrencia
- RowVersion/ETag para evitar pisar cambios (optimistic concurrency).
10.3 Naming conventions
- Código y DB en inglés.- Tablas: PascalCase o snake_case, definir 1 estándar.- Índices/constraints con nombres explícitos (evitar SYS_*).
11. Pagos (vista backend)
Se recomienda implementar Payments como módulo con 'provider adapters'. El dominio no debe depender de un proveedor específico.
11.1 Adapter pattern
- IPaymentProviderAdapter: CreatePaymentSession, VerifyWebhook, ParseEvent.- Implementación: BancardAdapter, PagoParAdapter (según priorización).- Configuración por tenant (post-MVP): credenciales por tenant o global según acuerdo.
11.2 Idempotencia
- Tabla PaymentEventsProcessed (Provider, ExternalEventId, ProcessedAt).- Si ya existe ExternalEventId: retornar 200 sin aplicar cambios.
12. Borrador de endpoints (MVP)
Tenants
- POST /api/tenants (SuperAdmin)
- GET /api/tenants/{id} (SuperAdmin)
- GET /api/store (context tenant)
Catalog
- POST /api/products
- PUT /api/products/{id}
- PATCH /api/products/{id}/publish
- PATCH /api/products/{id}/archive
- GET /api/products (admin)
- GET /api/public/products (storefront)
- GET /api/public/products/{slugOrId} (storefront)
Cart & Orders
- POST /api/cart/items
- PUT /api/cart/items/{id}
- DELETE /api/cart/items/{id}
- POST /api/checkout
- GET /api/orders (admin)
- GET /api/orders/{id} (admin)
Payments
- POST /api/payments (create session)
- POST /api/payments/webhook/{provider} (callback/webhook)
13. Roadmap técnico (backend)
- Fase 1: Host + pipeline (TenantContext, Auth, error handler, logging)
- Fase 2: Tenants + Catalog (CRUD + filtros tenant)
- Fase 3: Cart + Checkout + Orders
- Fase 4: Payments provider 1 + webhook idempotente
- Fase 5: Auditoría mínima + hardening (rate limit, security headers)
14. Diagramas (Mermaid)
14.1 Componentes – Backend modular monolith
```mermaidflowchart TB  Api[EShopy.Api] --> Tenants[Module: Tenants]  Api --> Catalog[Module: Catalog]  Api --> Carts[Module: Carts]  Api --> Orders[Module: Orders]  Api --> Payments[Module: Payments]  Api --> Identity[Module: Identity]  Tenants --> Infra[Infrastructure (EF/SQL)]  Catalog --> Infra  Carts --> Infra  Orders --> Infra  Payments --> Infra  Identity --> Keycloak[Keycloak]  Payments --> Provider[Payment Provider]  Infra --> Sql[(SQL Server)]```
14.2 Pipeline – Request
```mermaidflowchart LR  Req[HTTP Request] --> Corr[Correlation/Trace Middleware]  Corr --> Tenant[Resolve Tenant Middleware]  Tenant --> Auth[JWT Auth + Policies]  Auth --> Val[Validation + Normalization]  Val --> UC[Use Case Handler]  UC --> Db[(SQL Server)]  UC --> Resp[Standard Response]  Resp --> End[HTTP Response]```
Actualización v1.1 – Convención REST de Endpoints
Se actuala la convención de endpoints para adoptar un enfoque REST resource-oriented, evitando verbos en la URL (Add, List, Find, Enable, Disable). Los cambios de estado se realizan mediante PATCH.
Convención adoptada:
- POST /resources
- GET /resources
- GET /resources/{id}
- PUT /resources/{id}
- PATCH /resources/{id} o /resources/{id}/status
- DELETE /resources/{id} (preferir soft-delete)
Ejemplo aplicado (Actor):- POST /actors- GET /actors- GET /actors/{id}- PUT /actors/{id}- PATCH /actors/{id}/status- DELETE /actors/{id}

Actualización v1.2 – Convención REST Final y Naming de Endpoints
Esta sección establece de forma explícita y definitiva la convención REST adoptada por eShopy. Su objetivo es evitar estilos RPC (Add, Edit, Enable, Disable) y asegurar consistencia, previsibilidad y compatibilidad con OpenAPI y generación de clientes.
1. Principios REST adoptados
- Las URLs representan recursos, no acciones.
- Los verbos HTTP definen la acción (POST, GET, PUT, PATCH, DELETE).
- No se permiten verbos en el path (Add, List, Find, Enable, Disable).
- Las transiciones de estado se realizan con PATCH.
- DELETE representa borrado lógico (soft-delete) salvo indicación explícita.
2. Convención estándar de endpoints
Convención base válida para cualquier agregado del dominio:
POST   /resourcesGET    /resourcesGET    /resources/{id}PUT    /resources/{id}PATCH  /resources/{id}/statusDELETE /resources/{id}
3. Ejemplo aplicado (Actor)
Ejemplo concreto aplicado a un recurso Actor:
POST   /actorsGET    /actorsGET    /actors/{id}PUT    /actors/{id}PATCH  /actors/{id}/statusDELETE /actors/{id}
4. Búsquedas y filtros
No se definen endpoints separados para búsquedas (Find). Las búsquedas se realizan mediante query parameters:
GET /actors?q=...GET /actors?document=...GET /actors?status=Active
5. Multi-tenancy y seguridad
El tenant se resuelve exclusivamente por host/subdominio en middleware backend.- TenantId nunca se envía en path, body ni query params.- Todos los repositorios aplican filtros automáticos por TenantId.
6. Reglas explícitas (obligatorias)
- ❌ Prohibido crear endpoints tipo /Add, /Edit, /Enable, /Disable.
- ❌ Prohibido enviar TenantId desde el frontend.
- ❌ Prohibido exponer estados internos no documentados.
- ✅ Obligatorio documentar todo endpoint en OpenAPI.
- ✅ Obligatorio usar códigos HTTP correctos (400, 401, 403, 404, 409, 500).

## eShopy_Documento_4_Modelo_de_Datos_v1.0

eShopy – Documento 4: Modelo de Datos (ER + Naming + Índices + Constraints)
Versión: 1.0
Propósito: definir el modelo de datos inicial (MVP) para SQL Server, incluyendo entidades, relaciones (ER), convenciones de nombres, índices y constraints para soportar multi-tenancy por TenantId.
1. Convenciones
1.1 Idioma y estilo
- Todo en inglés (tablas, columnas, constraints, índices).
- Timestamps en UTC (datetime2).
- Identificadores tipo GUID (uniqueidentifier) para entidades principales (recomendado en SaaS).
1.2 Naming
- Schema: dbo (MVP). (Opcional futuro: schema 'eshopy').
- Tablas: PascalCase plural (ej. Tenants, Products, Orders).
- Columnas: PascalCase (ej. TenantId, CreatedAtUtc).
- PK: PK_<TableName>
- FK: FK_<ChildTable>_<ParentTable>_<ColumnName>
- UK (unique): UQ_<TableName>_<Columns>
- Índices: IX_<TableName>_<Columns>
- Checks: CK_<TableName>_<RuleName>
1.3 Columnas base (AppEntity)
Para todas las tablas multi-tenant:- Id (uniqueidentifier)- TenantId (uniqueidentifier)- CreatedAtUtc (datetime2)- CreatedBy (nvarchar(100))- UpdatedAtUtc (datetime2) NULL- UpdatedBy (nvarchar(100)) NULL- RowVersion (rowversion)- Data (nvarchar(max)) NULL  -- JSON para extensiones
2. Multi-tenancy (TenantId)
- Todas las tablas de negocio incluyen TenantId.- Toda clave única de negocio debe incluir TenantId.- Prohibido exponer TenantId como input directo en APIs (se resuelve por subdominio/token).- (Upgrade) Row-Level Security opcional post-MVP para hardening.
3. Entidades (MVP) – Descripción
- Tenants: Datos del comercio (tenant). Tabla global (no multi-tenant).
- Stores: Configuración de la tienda (branding mínimo) por tenant.
- TenantUsers: Perfil local de usuario administrativo (referencia a Keycloak).
- Products: Catálogo de productos por tenant.
- ProductImages: Imágenes de producto (solo metadata + URL/key).
- Carts: Carritos por tenant (anónimo o asociado a email).
- CartItems: Items del carrito.
- Orders: Pedidos (checkout).
- OrderItems: Items del pedido con snapshot de precio.
- Payments: Pagos por pedido (intención + estados).
- PaymentEventsProcessed: Eventos procesados (idempotencia de webhooks).
- AuditLogs: Auditoría app-level para acciones sensibles.
4. Definición de tablas (MVP)
Nota: la siguiente definición es conceptual y sirve como base para generar DDL. En el Documento 5 se puede entregar el script SQL completo.
Tenants
Columns:
- Id uniqueidentifier
- Subdomain nvarchar(63)
- LegalName nvarchar(200)
- DisplayName nvarchar(200)
- OwnerEmail nvarchar(256)
- Status tinyint  -- 0=PendingSetup,1=Active,2=Suspended
- CreatedAtUtc datetime2
- UpdatedAtUtc datetime2 NULL
- RowVersion rowversion
- Data nvarchar(max) NULL  -- JSON
Primary Key: PK_Tenants (Id)
Unique Constraints:
- UQ_Tenants_Subdomain (Subdomain)
- UQ_Tenants_OwnerEmail (OwnerEmail)
Indexes:
- IX_Tenants_Status (Status)
Check Constraints:
- CK_Tenants_Subdomain_Format (len between 3 and 63; no spaces)
Notes:
- Subdomain se usa para resolver TenantId en middleware.
- OwnerEmail es referencia inicial para invitar al TENANT_OWNER (Keycloak).
Stores
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier
- Name nvarchar(200)
- PublicEmail nvarchar(256) NULL
- PublicPhone nvarchar(50) NULL
- CurrencyCode char(3)  -- PYG
- Timezone nvarchar(64)  -- America/Asuncion
- PrimaryColor nvarchar(16) NULL
- LogoUrl nvarchar(500) NULL
- CreatedAtUtc datetime2
- CreatedBy nvarchar(100)
- UpdatedAtUtc datetime2 NULL
- UpdatedBy nvarchar(100) NULL
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_Stores (Id)
Foreign Keys:
- FK_Stores_Tenants_TenantId (TenantId -> Tenants.Id)
Unique Constraints:
- UQ_Stores_TenantId (TenantId)  -- 1 store por tenant (MVP)
Indexes:
- IX_Stores_TenantId (TenantId)
TenantUsers
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier
- KeycloakUserId nvarchar(64)  -- sub/uuid del usuario
- Email nvarchar(256)
- DisplayName nvarchar(200)
- Status tinyint  -- 0=Active,1=Disabled
- CreatedAtUtc datetime2
- CreatedBy nvarchar(100)
- UpdatedAtUtc datetime2 NULL
- UpdatedBy nvarchar(100) NULL
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_TenantUsers (Id)
Foreign Keys:
- FK_TenantUsers_Tenants_TenantId (TenantId -> Tenants.Id)
Unique Constraints:
- UQ_TenantUsers_TenantId_KeycloakUserId (TenantId, KeycloakUserId)
- UQ_TenantUsers_TenantId_Email (TenantId, Email)
Indexes:
- IX_TenantUsers_TenantId_Status (TenantId, Status)
Products
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier
- StoreId uniqueidentifier
- Sku nvarchar(64) NULL
- Slug nvarchar(128)
- Name nvarchar(200)
- Description nvarchar(max) NULL
- Price decimal(18,2)
- CurrencyCode char(3)
- Status tinyint  -- 0=Draft,1=Active,2=Archived
- StockOnHand int NULL  -- stock simple opcional
- CreatedAtUtc datetime2
- CreatedBy nvarchar(100)
- UpdatedAtUtc datetime2 NULL
- UpdatedBy nvarchar(100) NULL
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_Products (Id)
Foreign Keys:
- FK_Products_Tenants_TenantId (TenantId -> Tenants.Id)
- FK_Products_Stores_StoreId (StoreId -> Stores.Id)
Unique Constraints:
- UQ_Products_TenantId_Slug (TenantId, Slug)
- UQ_Products_TenantId_Sku (TenantId, Sku)  -- si Sku no es null (filtrado por app o índice filtrado)
Indexes:
- IX_Products_TenantId_Status (TenantId, Status)
- IX_Products_TenantId_Name (TenantId, Name)
Check Constraints:
- CK_Products_Price_Positive (Price >= 0)
- CK_Products_Stock_NonNegative (StockOnHand is null or StockOnHand >= 0)
Notes:
- Slug es el identificador público del storefront (SEO).
- Recomendado: índice único filtrado para Sku IS NOT NULL.
ProductImages
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier
- ProductId uniqueidentifier
- SortOrder int
- Url nvarchar(500)
- StorageKey nvarchar(300) NULL
- CreatedAtUtc datetime2
- CreatedBy nvarchar(100)
- UpdatedAtUtc datetime2 NULL
- UpdatedBy nvarchar(100) NULL
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_ProductImages (Id)
Foreign Keys:
- FK_ProductImages_Products_ProductId (ProductId -> Products.Id)
Indexes:
- IX_ProductImages_TenantId_ProductId (TenantId, ProductId)
- IX_ProductImages_ProductId_SortOrder (ProductId, SortOrder)
Carts
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier
- StoreId uniqueidentifier
- CartToken nvarchar(128)  -- token para carrito anónimo
- BuyerEmail nvarchar(256) NULL
- Status tinyint  -- 0=Open,1=ConvertedToOrder,2=Abandoned
- ExpiresAtUtc datetime2 NULL
- CreatedAtUtc datetime2
- CreatedBy nvarchar(100)
- UpdatedAtUtc datetime2 NULL
- UpdatedBy nvarchar(100) NULL
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_Carts (Id)
Foreign Keys:
- FK_Carts_Stores_StoreId (StoreId -> Stores.Id)
Unique Constraints:
- UQ_Carts_TenantId_CartToken (TenantId, CartToken)
Indexes:
- IX_Carts_TenantId_Status (TenantId, Status)
- IX_Carts_ExpiresAtUtc (ExpiresAtUtc)
Notes:
- CartToken es el vínculo con el storefront (cookie/localStorage).
- Alternativa: carrito server-side por session, pero token permite escalabilidad.
CartItems
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier
- CartId uniqueidentifier
- ProductId uniqueidentifier
- Quantity int
- UnitPrice decimal(18,2)  -- snapshot para mostrar
- CurrencyCode char(3)
- CreatedAtUtc datetime2
- CreatedBy nvarchar(100)
- UpdatedAtUtc datetime2 NULL
- UpdatedBy nvarchar(100) NULL
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_CartItems (Id)
Foreign Keys:
- FK_CartItems_Carts_CartId (CartId -> Carts.Id)
- FK_CartItems_Products_ProductId (ProductId -> Products.Id)
Unique Constraints:
- UQ_CartItems_CartId_ProductId (CartId, ProductId)  -- 1 item por producto en el carrito
Indexes:
- IX_CartItems_TenantId_CartId (TenantId, CartId)
Check Constraints:
- CK_CartItems_Quantity_Positive (Quantity > 0)
- CK_CartItems_UnitPrice_NonNegative (UnitPrice >= 0)
Orders
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier
- StoreId uniqueidentifier
- OrderNumber bigint  -- secuencia por tenant (ver nota)
- Status tinyint  -- 0=PendingPayment,1=Paid,2=Cancelled,3=Refunded
- BuyerName nvarchar(200)
- BuyerEmail nvarchar(256)
- BuyerPhone nvarchar(50) NULL
- BuyerDocument nvarchar(50) NULL
- ShippingAddress nvarchar(500) NULL
- Subtotal decimal(18,2)
- Total decimal(18,2)
- CurrencyCode char(3)
- CreatedAtUtc datetime2
- CreatedBy nvarchar(100)
- UpdatedAtUtc datetime2 NULL
- UpdatedBy nvarchar(100) NULL
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_Orders (Id)
Foreign Keys:
- FK_Orders_Stores_StoreId (StoreId -> Stores.Id)
Unique Constraints:
- UQ_Orders_TenantId_OrderNumber (TenantId, OrderNumber)
Indexes:
- IX_Orders_TenantId_Status_CreatedAtUtc (TenantId, Status, CreatedAtUtc)
- IX_Orders_TenantId_BuyerEmail (TenantId, BuyerEmail)
Check Constraints:
- CK_Orders_Totals (Total >= 0 and Subtotal >= 0 and Total >= Subtotal)
Notes:
- OrderNumber: se recomienda generar por tenant (secuencia) para que sea amigable al usuario.
- Puede implementarse con tabla TenantCounters o sequence + mapping por tenant.
OrderItems
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier
- OrderId uniqueidentifier
- ProductId uniqueidentifier NULL  -- puede quedar null si producto se borra/archiva
- ProductName nvarchar(200)  -- snapshot
- Sku nvarchar(64) NULL  -- snapshot
- Quantity int
- UnitPrice decimal(18,2)  -- snapshot
- LineTotal decimal(18,2)
- CurrencyCode char(3)
- CreatedAtUtc datetime2
- CreatedBy nvarchar(100)
- UpdatedAtUtc datetime2 NULL
- UpdatedBy nvarchar(100) NULL
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_OrderItems (Id)
Foreign Keys:
- FK_OrderItems_Orders_OrderId (OrderId -> Orders.Id)
Indexes:
- IX_OrderItems_TenantId_OrderId (TenantId, OrderId)
- IX_OrderItems_ProductId (ProductId)
Check Constraints:
- CK_OrderItems_Quantity_Positive (Quantity > 0)
- CK_OrderItems_UnitPrice_NonNegative (UnitPrice >= 0)
- CK_OrderItems_LineTotal (LineTotal = UnitPrice * Quantity)
Payments
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier
- OrderId uniqueidentifier
- Provider nvarchar(50)  -- 'Bancard'|'PagoPar'
- ExternalPaymentId nvarchar(128) NULL
- ExternalReference nvarchar(128) NULL
- Status tinyint  -- 0=Initiated,1=Authorized,2=Captured,3=Failed,4=Refunded
- Amount decimal(18,2)
- CurrencyCode char(3)
- ProviderPayload nvarchar(max) NULL  -- JSON minimal
- CreatedAtUtc datetime2
- CreatedBy nvarchar(100)
- UpdatedAtUtc datetime2 NULL
- UpdatedBy nvarchar(100) NULL
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_Payments (Id)
Foreign Keys:
- FK_Payments_Orders_OrderId (OrderId -> Orders.Id)
Unique Constraints:
- UQ_Payments_TenantId_ExternalPaymentId (TenantId, ExternalPaymentId)  -- si no es null (filtrado)
Indexes:
- IX_Payments_TenantId_Status_CreatedAtUtc (TenantId, Status, CreatedAtUtc)
- IX_Payments_OrderId (OrderId)
Check Constraints:
- CK_Payments_Amount_Positive (Amount >= 0)
PaymentEventsProcessed
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier
- Provider nvarchar(50)
- ExternalEventId nvarchar(128)
- ProcessedAtUtc datetime2
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_PaymentEventsProcessed (Id)
Unique Constraints:
- UQ_PaymentEventsProcessed_TenantId_Provider_ExternalEventId (TenantId, Provider, ExternalEventId)
Indexes:
- IX_PaymentEventsProcessed_ProcessedAtUtc (ProcessedAtUtc)
AuditLogs
Columns:
- Id uniqueidentifier
- TenantId uniqueidentifier NULL  -- puede ser null para eventos globales
- EntityName nvarchar(100)
- EntityId nvarchar(64)
- Action nvarchar(50)  -- Create|Update|Delete|StatusChange|Webhook
- BeforeJson nvarchar(max) NULL
- AfterJson nvarchar(max) NULL
- UserId nvarchar(64) NULL
- UserEmail nvarchar(256) NULL
- TraceId nvarchar(100) NULL
- CreatedAtUtc datetime2
- RowVersion rowversion
- Data nvarchar(max) NULL
Primary Key: PK_AuditLogs (Id)
Indexes:
- IX_AuditLogs_TenantId_CreatedAtUtc (TenantId, CreatedAtUtc)
- IX_AuditLogs_EntityName_EntityId (EntityName, EntityId)
Notes:
- No guardar datos sensibles; truncar/filtrar payloads de proveedores.
- Puede crecer rápido: considerar partición lógica por fecha (post-MVP).
5. Índices y constraints críticos (resumen)
5.1 Unicidad por tenant
- Products: (TenantId, Slug) UNIQUE
- Orders: (TenantId, OrderNumber) UNIQUE
- Carts: (TenantId, CartToken) UNIQUE
- Users: (TenantId, Email) UNIQUE
- (Opcional) Products: unique filtered index en (TenantId, Sku) WHERE Sku IS NOT NULL
5.2 Performance (consultas típicas)
- Listar productos por tenant y estado -> IX_Products_TenantId_Status
- Buscar producto por slug en storefront -> UQ_Products_TenantId_Slug
- Listar pedidos por estado/fecha -> IX_Orders_TenantId_Status_CreatedAtUtc
- Webhook idempotente -> UQ_PaymentEventsProcessed_TenantId_Provider_ExternalEventId
6. Diagrama ER (Mermaid)
```mermaiderDiagram  TENANTS ||--|| STORES : has  TENANTS ||--o{ TENANTUSERS : contains  STORES  ||--o{ PRODUCTS : offers  PRODUCTS ||--o{ PRODUCTIMAGES : has  STORES ||--o{ CARTS : owns  CARTS ||--o{ CARTITEMS : contains  PRODUCTS ||--o{ CARTITEMS : references  STORES ||--o{ ORDERS : receives  ORDERS ||--o{ ORDERITEMS : contains  ORDERS ||--o{ PAYMENTS : paid_by  PAYMENTS ||--o{ PAYMENTEVENTSPROCESSED : idempotency```
7. Notas de implementación (EF Core / SQL Server)
- Usar RowVersion como token de concurrencia (IsRowVersion).
- Global Query Filter por TenantId en todas las tablas multi-tenant.
- Interceptor SaveChanges para setear CreatedAtUtc/UpdatedAtUtc y validar TenantId.
- Para OrderNumber por tenant: tabla TenantCounters o procedimiento atomizado.
- Para Sku y ExternalPaymentId: preferir índices únicos filtrados (WHERE ... IS NOT NULL).

## eShopy_Documento_5_Plan_de_Trabajo_Backend_v1.0

eShopy – Documento 5: Plan de Trabajo Backend (TODO Formal)
Versión: 1.0
Propósito: definir el plan de trabajo formal del backend para el MVP (Plan Básico) de eShopy. Este documento organiza las tareas técnicas (TODOs) por fases, módulos y responsabilidades, sirviendo como guía de implementación, backlog técnico y base para prompts de desarrollo asistido.
FASE 0 – Fundaciones del Backend
- Crear solución EShopy.Backend y estructura de proyectos.
- Configurar dependencias y referencias entre proyectos.
- Definir convenciones de nombres y estructura de carpetas.
- Configurar entornos (Development / QA / Production).
FASE 1 – Contexto y Middleware
- Implementar TenantContext y resolución de tenant por subdominio.
- Middleware de CorrelationId y propagación de TraceId.
- Middleware global de manejo de excepciones.
- Contrato estándar de errores para el frontend.
FASE 2 – Seguridad e Identidad
- Integración OIDC con Keycloak.
- Mapeo de claims a UserContext.
- Definición de roles y políticas RBAC.
- Handlers de autorización por módulo.
FASE 3 – Modelo Base y Persistencia
- Implementar AppEntity con auditoría y concurrencia.
- Configurar interceptores EF Core (TenantId, fechas UTC, auditoría).
- Definir repositorios base y unit of work.
FASE 4 – Tenants (Onboarding)
- Implementar TenantEntity (no multi-tenant).
- Repositorio de tenants.
- Caso de uso CreateTenant (onboarding).
- Integración con Keycloak para usuario Owner.
- Estados del tenant (PendingSetup, Active, Suspended).
FASE 5 – Catálogo
- Implementar ProductEntity y reglas de dominio.
- Casos de uso: crear, editar, publicar y archivar productos.
- Validaciones backend completas.
- Auditoría de cambios de precio y estado.
FASE 6 – Carrito
- Implementar CartEntity y CartItemEntity.
- Casos de uso: agregar, actualizar y eliminar ítems.
- Reglas de cantidad, stock y precio.
FASE 7 – Pedidos (Checkout)
- Implementar OrderEntity y OrderItemEntity.
- Caso de uso Checkout.
- Generación de OrderNumber por tenant.
- Transiciones de estado controladas.
FASE 8 – Pagos
- Implementar PaymentEntity.
- Definir IPaymentProviderAdapter.
- Implementar proveedor de pago inicial.
- Endpoints de webhook con idempotencia.
FASE 9 – Observabilidad
- Configurar logging estructurado con Serilog.
- Enrichers: TenantId, UserId, CorrelationId.
- Implementar auditoría app-level.
FASE 10 – Testing Strategy
- Tests unitarios de validadores y reglas de dominio.
- Tests de integración de endpoints.
- Tests de seguridad (RBAC).
- Tests de pagos y webhooks.
Notas finales
Este plan debe ejecutarse de forma incremental por fases. Cada fase produce entregables verificables y testeables. El frontend depende de contratos estables definidos en estas fases, pero no gobierna las validaciones ni reglas de negocio.

## eShopy_Documento_6_Flujo_de_Suscripcion_y_Billing_v1.1

eShopy – Documento 6: Flujo de Suscripción y Billing (Plan Básico)
Versión: 1.0
Propósito: definir el flujo completo de suscripción y facturación (billing) para el Plan Básico de eShopy, incluyendo onboarding del tenant, autenticación inicial, pago de la suscripción y activación automática del servicio.
1. Principios del flujo
- Onboarding self-service, sin intervención manual.
- Backend gobierna todo el flujo y las validaciones.
- Frontend solo guía al usuario (UX), no decide.
- Pago obligatorio para activar el tenant.
- Estados explícitos y auditables en cada paso.
2. Actores
- Prospect: usuario que aún no es tenant.
- Tenant Owner: propietario del comercio.
- eShopy Backend.
- Identity Provider (Google via Keycloak).
- Payment Provider (Bancard / PagoPar).
3. Estados del Tenant
- Draft: registro iniciado, sin autenticación.
- PendingPayment: autenticado, suscripción aún no pagada.
- Active: suscripción paga, tenant operativo.
- Suspended: falta de pago o bloqueo administrativo.
4. Flujo principal de suscripción (happy path)
1. Prospect ingresa a eshopy.com.py y selecciona 'Crear mi tienda'.
2. Frontend inicia login con Google (OIDC) vía Keycloak.
3. Backend recibe token validado y crea usuario provisional.
4. Prospect completa datos del comercio y subdominio deseado.
5. Backend valida subdominio y crea Tenant en estado PendingPayment.
6. Backend crea Store mínima asociada al tenant.
7. Backend inicia pago de suscripción (Plan Básico).
8. Prospect completa el pago en la pasarela.
9. Payment Provider notifica pago exitoso (webhook).
10. Backend valida webhook (firma + idempotencia).
11. Tenant pasa a estado Active.
12. Se asigna rol TENANT_OWNER al usuario.
13. Se habilita acceso al Admin del tenant.
5. Flujos alternativos y errores
- Subdominio ya existe: se rechaza y se solicita uno nuevo.
- Pago rechazado o cancelado: tenant permanece en PendingPayment.
- Webhook inválido o duplicado: se ignora y se audita.
- Falla en Keycloak: tenant queda en PendingPayment con reintento manual.
6. Modelo de Suscripción
- Tipo: mensual.
- Plan inicial: Plan Básico.
- Renovación automática (si provider lo permite).
- Sin prorrateo en MVP.
- Un tenant tiene una sola suscripción activa.
7. Entidades involucradas (conceptual)
- Tenant (estado, owner).
- Store (configuración mínima).
- Subscription (plan, estado, fechas).
- Payment (transacción de suscripción).
- TenantUser (Owner).
8. Seguridad
- Autenticación obligatoria con Google.
- Validación de webhook por firma/secret.
- No confiar en callbacks del frontend para estados de pago.
- Auditoría de cada transición de estado.
9. Observabilidad
- Logs por cada paso del flujo con CorrelationId.
- AuditLog por creación y cambio de estado del tenant.
- Métricas: tasa de conversión, pagos fallidos.
10. Endpoints involucrados (borrador)
- POST /api/onboarding/start
- POST /api/onboarding/tenant
- POST /api/subscriptions/start
- POST /api/payments/webhook/subscription
11. Notas para planes futuros
- Plan Gold / Diamond con upgrades de suscripción.
- Facturación electrónica.
- Suspensión automática por falta de pago.
- Portal de billing para el tenant.
Actualización v1.1 – Naming REST de Endpoints
Los endpoints de onboarding y billing se alinean a convención REST:
- POST /onboarding/tenants
- POST /subscriptions
- POST /subscriptions/{id}/payments
- POST /payments/webhooks/subscription

## eShopy_Documento_7_Testing_Strategy_v1.0

eShopy – Documento 7: Testing Strategy (Unitarios, Integración, Pagos)
Versión: 1.0
Propósito: definir una estrategia de pruebas para el backend del MVP (Plan Básico) de eShopy, asegurando calidad, consistencia y robustez en reglas de negocio, multi-tenancy, seguridad y pagos.
1. Principios
- Backend gobierna reglas: la mayor cobertura debe estar en Application/Domain.
- Pocas pruebas end-to-end pero bien elegidas; muchas pruebas unitarias rápidas.
- Integración real con EF/SQL Server en tests para validar filtros de tenant e integridad.
- Pagos siempre testeados con simulación determinística e idempotencia.
- Cada bug crítico produce un test de regresión.
2. Pirámide de pruebas (recomendada)
- Unit tests: 70–80%
- Integration tests: 20–25%
- E2E (post-MVP): 5–10% (Playwright/Cypress)
3. Herramientas y librerías sugeridas
- xUnit (tests)
- FluentAssertions (asserts legibles)
- NSubstitute o Moq (mocks)
- Testcontainers for .NET (SQL Server) o LocalDB (MVP)
- Respawn (reset DB en integration tests)
- WireMock.Net (simular providers externos)
4. Unit Tests
4.1 Qué cubrir
- Validators (FluentValidation): campos requeridos, formatos, normalización.
- Domain rules/invariants: estados válidos, transiciones, reglas de cálculo.
- Use cases handlers: flujos UC-xx con mocks de repos/adapters.
- Authorization handlers/policies: roles y permisos.
- Tenant resolution (parsing subdomain) sin DB (unit).
4.2 Casos unitarios mínimos (MVP)
- UC-01 CreateTenant: subdomain válido/ inválido, duplicado, rollback lógico cuando falla Keycloak.
- Catalog: Create/UpdateProduct valida price>=0, slug único, status transitions (Draft->Active, Active->Archived).
- Cart: AddItem valida producto Active y quantity>0; UpdateItem ajusta stock si aplica.
- Checkout: copia snapshot de price a OrderItem; total >= subtotal; estado inicial PendingPayment.
- Payments: transición Payment/Order solo con eventos válidos; idempotencia por ExternalEventId.
- Error mapping: DomainException -> 409; Validation -> 400; Unexpected -> 500.
5. Integration Tests
5.1 Qué cubrir
- Pipeline completo: middlewares (CorrelationId, TenantResolution, Auth) + endpoints.
- EF Core Global Query Filters por TenantId (no leaks).
- Concurrencia (RowVersion) en updates.
- Persistencia real de Orders/Payments y sus relaciones.
- Logging: smoke test de propiedades clave (TenantId, TraceId) en eventos principales.
5.2 Setup recomendado (SQL Server)
- Usar Testcontainers con SQL Server para pruebas realistas.- Alternativa MVP: LocalDB.- Usar Respawn para limpiar estado entre tests.- Seeds controlados por test (tenantA, tenantB) para validar aislamiento.
5.3 Casos de integración mínimos
- Tenant isolation: crear Product en tenantA y confirmar que tenantB no lo ve.
- Endpoint /api/public/products/{slug}: resuelve tenant por host y retorna solo ese tenant.
- Checkout + Payment initiation: crea Order PendingPayment y Payment Initiated.
- Optimistic concurrency: dos updates simultáneos de Product -> conflicto esperado.
- Webhook endpoint: firma inválida -> 401/403; evento duplicado -> 200 sin cambios.
6. Testing específico de Pagos
6.1 Estrategia
- No depender del provider real en tests.
- Mock adapter (IPaymentProviderAdapter) para unit tests.
- Simulación HTTP (WireMock.Net) para integration tests.
- Validar idempotencia obligatoria con PaymentEventsProcessed.
- Validar seguridad: firma/secret y validación de payload.
6.2 Matriz de escenarios (mínimo)
- Pago exitoso: webhook -> Payment Captured + Order Paid.
- Pago fallido: webhook -> Payment Failed; Order sigue PendingPayment o Cancelled según regla.
- Webhook duplicado: no duplica transiciones ni logs críticos.
- Webhook inválido: no cambia estado; audita evento de seguridad.
- Timeout provider al crear sesión: Payment Failed; reintento permitido.
7. Organización y convención
- Proyecto: EShopy.Tests.Unit (xUnit)
- Proyecto: EShopy.Tests.Integration (xUnit)
- Carpeta por módulo: Tenants, Catalog, Carts, Orders, Payments, Identity, Infrastructure
- Convención de naming: <UnitUnderTest>_<Scenario>_<Expected>
8. Quality Gates (CI)
- Build + Unit tests en cada PR.
- Integration tests en main/develop (o nightly si son pesados).
- Cobertura mínima por módulos críticos (Payments, Tenants) definida por objetivo.
- Análisis estático opcional MVP, recomendado post-MVP.
9. Checklist de regresión (antes de release)
- Multi-tenant: no leaks entre tenants en endpoints públicos y admin.
- Auth/RBAC: policies funcionan; no hay endpoints admin sin autorización.
- Checkout: totals consistentes y snapshot de precios en OrderItems.
- Pagos: webhook idempotente y seguro.
- Errores: contrato estable y traceId presente.
- Logs: TenantId y CorrelationId presentes en eventos principales.

## eShopy_Documento_8_Arquitectura_Frontend_Angular_v1.0

eShopy – Documento 8: Arquitectura Frontend (Angular + Theming + Responsive)
Versión: 1.0
Propósito: definir la arquitectura del frontend para el MVP (Plan Básico) de eShopy, incluyendo estructura de aplicación Angular, estrategia de UI/UX responsive, componentes reutilizables, theming centralizado y alineación con modelos/contratos del backend.
1. Decisión de arquitectura: ¿Microfrontend?
Recomendación para MVP (equipo de 1 persona): NO microfrontend. Usar un monorepo o repo único con modularización interna. Microfrontend agrega complejidad (build, routing, shared deps, versionado) que no aporta valor al inicio.
Criterio para evaluar microfrontend en el futuro:
- Múltiples equipos trabajando en dominios distintos (Admin vs Storefront).
- Requerimiento de despliegues independientes por módulo.
- Necesidad real de aislar ciclos de release por feature.
Plan recomendado: iniciar con 2 aplicaciones en un mismo workspace (Admin + Storefront) compartiendo una librería de UI y una librería de API-client. Esto da el 90% del beneficio sin el costo.
2. Aplicaciones frontend (MVP)
Se recomiendan 2 apps Angular separadas dentro del mismo workspace:- eShopy Admin: panel del tenant (catalog, orders, settings)- eShopy Storefront: tienda pública (catalog, product, cart, checkout)Ambas comparten librerías internas de UI, theming y acceso a API.
3. Stack y librerías
- Angular (última LTS disponible al iniciar).
- Angular Standalone Components (sin NgModules cuando sea posible).
- Angular Router (lazy loading por features).
- State Management: NgRx (si crece) o Signals + servicios (MVP).
- HTTP: HttpClient + interceptors.
- UI: Angular Material (rápido, consistente) o una capa propia sobre Material.
- Formularios: Reactive Forms + validación mínima UX.
- Testing: Jest (unit) + Playwright (E2E post-MVP).
4. Estructura de workspace (propuesta)
Estructura sugerida (monorepo Angular):- apps/  - admin/  - storefront/- libs/  - ui/            (componentes reutilizables + estilos)  - theme/         (paleta, tokens, tipografía)  - api-client/    (SDK del backend generado o escrito)  - auth/          (Keycloak, guards, interceptors)  - shared/        (utils, pipes, validators UX)
5. Theming centralizado (no romper armonía)
Objetivo: evitar que cada módulo invente colores/estilos. Todo el diseño debe consumir tokens centralizados.
5.1 Tokens recomendados (Theme Tokens)
- Colors: primary, secondary, surface, text, success, warning, error.
- Typography: font family, sizes, weights, line heights.
- Spacing: escala 4/8/12/16/24/32.
- Radius: 8/12/16.
- Shadows: 1–3 niveles.
5.2 Implementación sugerida- Definir tokens en CSS variables (':root') por app.- Para el tenant: permitir override controlado en Storefront (PrimaryColor, Logo, etc.) desde Store settings.- La UI library debe usar SOLO variables/tokens (nunca colores hardcode).
6. Componentes reutilizables (libs/ui)
Lista de componentes base a estandarizar desde el inicio:
- AppButton (variants, loading, disabled, single-click guard)
- AppTextField / AppSelect / AppAutocomplete (errores estándar, hints)
- AppToast / AppSnackbar (mensajes unificados)
- AppDialog (confirmaciones)
- AppDataGrid (tabla con paginación, sorting, empty state)
- AppLoading (skeleton/spinner)
- AppPageLayout (header + content + actions)
Nota: aunque el backend valida todo, el frontend debe incluir validaciones UX mínimas (required, email format, length) para reducir fricción, sin duplicar reglas complejas.
7. Responsive (Admin y Storefront)
7.1 Principios
- Mobile-first para Storefront (la mayoría compra desde móvil).
- Admin responsive, pero optimizado para desktop.
- Layout por breakpoints: xs <600, sm 600–960, md 960–1280, lg >1280.
- Evitar tablas complejas en móvil: usar cards + filtros.
7.2 Patrones sugeridos
- Storefront: catálogo en grid responsive, detalle con CTA claro, carrito como drawer.
- Checkout: stepper simple o 1 página con secciones colapsables.
- Admin: sidebar colapsable, topbar con tenant switch (si aplica), grids con filtros.
8. Auth, tenant y seguridad frontend
8.1 Resolución de tenant
- El tenant se resuelve por el host/subdominio. El frontend NO envía TenantId en requests. El backend decide TenantId por middleware.
8.2 Autenticación
- Admin: OIDC Authorization Code + PKCE via Keycloak.- Storefront: anónimo por defecto; auth buyer post-MVP.
8.3 Interceptors
- AuthInterceptor: agrega Authorization Bearer al Admin.
- CorrelationIdInterceptor: agrega X-Correlation-Id en todas las requests.
- ErrorInterceptor: normaliza errores del backend y muestra toast estándar.
- Retry (solo para GET idempotentes) opcional.
8.4 Anti doble click / isSubmitting
- En botones críticos (checkout, pay, save): usar estado loading y bloquear múltiple submit.- El backend debe ser idempotente donde corresponda (payments).
9. Modelado de entidades y contratos (frontend)
Recomendación: NO modelar Entities del backend. El frontend debe usar DTOs/Contracts.
9.1 Estrategia recomendada
- Definir contratos OpenAPI en el backend (Documentación 9 futuro).
- Generar cliente TypeScript (api-client) automáticamente desde OpenAPI.
- Usar tipos separados por contexto: Admin DTOs y Storefront DTOs.
- Evitar 'mega modelos' compartidos; preferir view-models por pantalla cuando convenga.
9.2 Convenciones
- Tipos TS en inglés.
- Responses siempre envueltas en Result/PagedResult si aplica.
- Fechas ISO UTC; convertir a zona local solo en UI.
10. Routing y modularización por features
Admin (lazy routes sugeridas):
- /dashboard
- /catalog (products, images)
- /orders (list, detail)
- /settings (store settings, users)
- /billing (post-MVP)
Storefront (lazy routes sugeridas):
- / (home/catalog)
- /p/:slug (product detail)
- /cart
- /checkout
- /payment/return (provider return page)
11. Testing frontend (mínimo)
MVP: enfocar en componentes críticos y flujos principales.- Unit: pipes, validators UX, servicios API-client wrapper.- Integration UI (post-MVP): Playwright para Storefront checkout happy path.
12. Entregables siguientes
- Documento: UI Kit (tokens + componentes) con ejemplos.- Documento: Contratos OpenAPI y generación de cliente TS.- Documento: Flujo de navegación y wireframes base (Admin/Storefront).

## eShopy_Documento_9_Plan_Gold_Diamond_v1.0

eShopy – Documento 9: Planes Gold y Diamond (Producto y Alcance)
Versión: 1.0
Propósito: definir el alcance funcional y de producto de los planes Gold y Diamond de eShopy. Este documento es deliberadamente no técnico en implementación y evita promesas vagas; describe qué incluye cada plan y para qué tipo de cliente está pensado.
1. Principios de definición de planes
- Cada plan agrega valor real y medible respecto al anterior.
- No se repiten funcionalidades: se desbloquean capacidades.
- El Plan Básico es suficiente para vender; Gold/Diamond escalan el negocio.
- Todo lo no incluido queda explícitamente fuera del alcance.
2. Público objetivo por plan
- Plan Básico: emprendedores y pymes pequeñas.
- Plan Gold: negocios en crecimiento con operación diaria.
- Plan Diamond: empresas con volumen, equipos y necesidades avanzadas.
3. Plan Gold
Objetivo: profesionalizar la operación y aumentar conversión/eficiencia.
3.1 Funcionalidades incluidas
- Inventario avanzado (movimientos, mínimos, alertas).
- Gestión de clientes (Customer): historial de compras y datos.
- Cupones y promociones básicas.
- Reportes operativos (ventas, productos más vendidos).
- Múltiples usuarios staff con permisos más granulares.
- Integración con WhatsApp (link y mensajes transaccionales básicos).
- Soporte prioritario (SLA mejorado).
3.2 Limitaciones explícitas
- Una sola tienda por tenant.
- Sin multi-sucursal.
- Sin integraciones contables.
- Sin personalizaciones visuales profundas.
4. Plan Diamond
Objetivo: escalar operación, automatizar y adaptarse a procesos empresariales.
4.1 Funcionalidades incluidas
- Multi-sucursal / multi-almacén.
- Facturación electrónica (cuando aplique normativa).
- Integraciones contables (exportación o API).
- Flujos avanzados de roles y aprobaciones.
- Personalización avanzada del storefront (branding extendido).
- Múltiples pasarelas de pago.
- Ambiente de staging/sandbox por tenant.
- Backups y retención extendida.
- Soporte premium y acompañamiento.
4.2 Limitaciones explícitas
- No incluye desarrollo a medida fuera del roadmap.
- Integraciones específicas sujetas a viabilidad técnica/comercial.
5. Comparativo resumido
Funcionalidad
Básico
Gold
Diamond
Venta online
✔
✔
✔
Inventario avanzado
—
✔
✔
Clientes (CRM básico)
—
✔
✔
Cupones/promos
—
✔
✔
Reportes
—
✔
✔
Multi-sucursal
—
—
✔
Facturación electrónica
—
—
✔
Integraciones contables
—
—
✔
Soporte
Básico
Prioritario
Premium
6. Notas comerciales
- El upgrade de plan debe ser inmediato y sin pérdida de datos.
- Las funcionalidades se activan por configuración, no por despliegue.
- Los precios se definen fuera de este documento.
- El downgrade puede implicar desactivación (no borrado) de features.
7. Fuera de alcance general (todos los planes)
- Desarrollo a medida fuera del producto.
- Soporte a hardware o infraestructura del cliente.
- Capacitación presencial.
