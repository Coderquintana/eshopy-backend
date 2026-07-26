# Architecture — API Contracts

> Contratos de endpoints, DTOs y códigos de error. Base URL: `/api`.

## Convenciones globales

- Content-Type: `application/json; charset=utf-8`
- Fechas: ISO 8601 UTC (`2026-02-19T13:45:00Z`)
- TenantId: **nunca** en request body ni query param — resuelto por host/subdominio
- Paginación: query params `page` (default 1) + `pageSize` (default 20)
- Auth: `Authorization: Bearer <jwt_token>` en headers

## ErrorResponse (estándar)

```json
{
  "traceId": "00-abc123-def456-00",
  "code": "NOT_FOUND",
  "message": "Producto no encontrado.",
  "details": {}
}
```

## Códigos de error canónicos

| Código | HTTP | Descripción |
|---|---|---|
| `VALIDATION_ERROR` | 400 | Error de validación (FluentValidation) |
| `TENANT_NOT_FOUND` | 404 | Tenant no encontrado para el subdominio |
| `UNAUTHORIZED` | 401 | Token ausente o inválido |
| `FORBIDDEN` | 403 | Sin permisos (token válido pero policy falla) |
| `NOT_FOUND` | 404 | Recurso no encontrado |
| `CONFLICT` | 409 | Slug/SKU/subdominio duplicado u otro conflicto de unicidad |
| `CONCURRENCY_CONFLICT` | 409 | El recurso fue modificado por otro proceso (RowVersion) |
| `PRODUCT_NOT_AVAILABLE` | 409 | Producto no disponible para agregar al carrito |
| `PRODUCT_INVALID_STATE` | 409 | Transición de estado de producto no permitida |
| `TENANT_INVALID_STATE` | 409 | Transición de estado de tenant/suscripción no permitida |
| `TENANT_SUSPENDED` | 403 | El tenant esta suspendido (mora) — bloqueado por `TenantResolutionMiddleware` |
| `TENANT_CANCELLED` | 403 | El tenant fue cancelado — bloqueado por `TenantResolutionMiddleware` |
| `EXTERNAL_SERVICE_ERROR` | 502 | Falla al comunicarse con un servicio externo (ej. Keycloak Admin API) |
| `ORDER_INVALID_STATE` | 409 | Transición de orden/pago no permitida |
| `PAYMENT_WEBHOOK_INVALID` | 401 | Webhook con firma inválida |
| `PAYMENT_PROVIDER_ERROR` | 502 | Error al comunicarse con el provider de pago (diseño — sin uso real hasta que exista un adapter real de Bancard/PagoPar) |
| `GENERIC_ERROR` | 500 | Error interno no controlado |

---

## Catalog — Admin endpoints

### POST /api/products
Crear producto en estado Draft.

**Auth**: `CatalogWrite`

**Request body:**
```json
{
  "slug": "remera-blanca",
  "sku": "REM-001",
  "name": "Remera Blanca",
  "description": "Algodón 100%.",
  "price": 85000,
  "stockOnHand": 50
}
```

> `CurrencyCode` NO se envía — el backend lo toma del Store.

**Response 201:**
```json
{
  "id": "aaaaaaaa-...",
  "slug": "remera-blanca",
  "sku": "REM-001",
  "name": "Remera Blanca",
  "description": "Algodón 100%.",
  "price": 85000,
  "currencyCode": "PYG",
  "status": "Draft",
  "stockOnHand": 50,
  "createdAtUtc": "2026-02-19T13:00:00Z",
  "updatedAtUtc": "2026-02-19T13:00:00Z"
}
```

---

### GET /api/products
Listar productos admin con paginación.

**Auth**: `CatalogWrite`
**Query**: `?page=1&pageSize=20`

**Response 200:**
```json
{
  "items": [ { /* ProductAdminDto */ } ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8
}
```

---

### GET /api/products/{id:guid}
Detalle de producto por ID.

**Auth**: `CatalogWrite`
**Response 200**: `ProductAdminDto`

---

### PUT /api/products/{id:guid}
Actualizar producto.

**Auth**: `CatalogWrite`

**Request body:**
```json
{
  "name": "Remera Blanca XL",
  "description": "Talle extra grande.",
  "price": 90000,
  "stockOnHand": 30,
  "sku": "REM-001-XL"
}
```

**Response 200**: `ProductAdminDto` actualizado

---

### PATCH /api/products/{id:guid}/status
Cambiar estado del producto.

**Auth**: `CatalogWrite`

**Request body:**
```json
{ "status": 1 }
```
> Valores: 0=Draft, 1=Active, 2=Archived

**Response 200**: `ProductAdminDto` con nuevo estado

---

## Catalog — Storefront endpoints

### GET /api/public/products
Listar productos activos (public).

**Auth**: Anónimo
**Query**: `?page=1&pageSize=20`

**Response 200:**
```json
{
  "items": [
    {
      "id": "...",
      "slug": "remera-blanca",
      "name": "Remera Blanca",
      "description": "Algodón 100%.",
      "price": 85000,
      "currencyCode": "PYG"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 5,
  "totalPages": 1
}
```

---

### GET /api/public/products/{slug}
Detalle de producto público por slug.

**Auth**: Anónimo
**Response 200**: `ProductPublicDto`

---

## Tenants / Store — Onboarding

### POST /api/onboarding/tenants
Crea un tenant nuevo: Tenant (PendingPayment) + Store (defaults) + usuario Owner en Keycloak +
Subscription (PendingActivation). Excluido de `TenantResolutionMiddleware` (no requiere subdominio).

**Auth**: Público

**Request body:**
```json
{
  "subdomain": "mitienda",
  "businessName": "Mi Tienda SRL",
  "ownerEmail": "dueño@mitienda.com",
  "ownerName": "Juan Pérez",
  "plan": "basic"
}
```

**Response 201:**
```json
{
  "tenantId": "aaaaaaaa-...",
  "subdomain": "mitienda",
  "status": "PendingPayment"
}
```

> No incluye `paymentUrl`: el modulo de Payments (Fase 8) todavia no existe. Ver
> `POST /api/admin/tenants/{id}/activate` para la activacion disponible hoy.

**Errores**: `VALIDATION_ERROR` (400), `CONFLICT` si el subdominio ya existe (409),
`EXTERNAL_SERVICE_ERROR` si falla la creacion del usuario en Keycloak (502).

---

### GET /api/admin/tenants/{id:guid}
Detalle de un tenant. Operacion a nivel plataforma — excluido de `TenantResolutionMiddleware`.

**Auth**: `TenantsRead` (SUPERADMIN)

**Response 200:**
```json
{
  "id": "aaaaaaaa-...",
  "subdomain": "mitienda",
  "businessName": "Mi Tienda SRL",
  "status": "Active",
  "plan": "Basic",
  "createdAtUtc": "2026-07-26T13:00:00Z",
  "activatedAtUtc": "2026-07-26T13:05:00Z"
}
```

---

### POST /api/admin/tenants/{id:guid}/activate
Activa manualmente un tenant en `PendingPayment` (o lo reactiva desde `Suspended`). Herramienta de
soporte/ops permanente — hasta que exista el webhook de pago (Fase 8), es la unica forma de activar
un tenant. Excluido de `TenantResolutionMiddleware`.

**Auth**: `TenantsWrite` (SUPERADMIN)

**Response 200**: `TenantAdminDto` con `status = "Active"`.

**Errores**: `NOT_FOUND` si el tenant o su suscripcion no existen (404),
`TENANT_INVALID_STATE` si la transicion no es valida, ej. tenant ya `Cancelled` (409).

---

## Tenants / Store — Usuarios del tenant

### GET /api/admin/users
Lista los usuarios (Owner, Admin, Staff) del tenant resuelto por subdominio.

**Auth**: `UsersManage` (SUPERADMIN u OWNER)

**Response 200:**
```json
[
  { "id": "...", "email": "owner@mitienda.com", "name": "Juan Perez", "role": "Owner", "isActive": true, "createdAtUtc": "2026-07-26T13:00:00Z" },
  { "id": "...", "email": "staff@mitienda.com", "name": "Ana Gomez", "role": "Staff", "isActive": true, "createdAtUtc": "2026-07-26T14:00:00Z" }
]
```

---

### POST /api/admin/users
Invita un usuario Admin o Staff al tenant actual (crea el usuario en Keycloak). El Owner **no** es
invitable por esta via: se crea una unica vez durante `POST /api/onboarding/tenants`.

**Auth**: `UsersManage` (SUPERADMIN u OWNER)

**Request body:**
```json
{
  "email": "staff@mitienda.com",
  "name": "Ana Gomez",
  "role": "staff"
}
```
> `role`: `"admin"` o `"staff"` (case-insensitive).

**Response 201**: `TenantUserDto` (ver arriba).

**Errores**: `VALIDATION_ERROR` (400, incluye `role: "owner"`), `CONFLICT` si el email ya existe
en este tenant (409), `EXTERNAL_SERVICE_ERROR` si falla la creacion en Keycloak (502).

---

## Store

### GET /api/store
Configuración pública del store, resuelto por subdominio.

**Auth**: Anónimo

**Response 200:**
```json
{
  "storeId": "22222222-...",
  "name": "Mi Tienda",
  "currencyCode": "PYG",
  "timezone": "America/Asuncion",
  "primaryColor": "#007bff",
  "logoUrl": "https://...",
  "backgroundColor": "#FFFFFF",
  "description": "Una tienda de ejemplo"
}
```

---

### PUT /api/store
Actualiza el perfil de la tienda (nombre, timezone, branding). `CurrencyCode` no es editable: cambiarla
rompería precios ya registrados en Products/Orders.

**Auth**: `StoreWrite`

**Request body:**
```json
{
  "name": "Mi Tienda",
  "timezone": "America/Asuncion",
  "primaryColor": "#007bff",
  "logoUrl": "https://cdn.example.com/logo.png",
  "backgroundColor": "#FFFFFF",
  "description": "Una tienda de ejemplo"
}
```

**Response 200**: `StoreProfileDto` actualizado.

---

## Cart

Todos los endpoints usan el header `X-Cart-Token` (UUID generado en el frontend) para identificar
el carrito — no hay auth, es anónimo por diseño.

### GET /api/cart
Obtiene el carrito actual. Si el `X-Cart-Token` no tiene carrito asociado todavia, retorna uno vacio
(200, no 404) — un carrito sin items es un estado valido, no un error.

**Response 200:** `CartDto` (ver DTOs de referencia).

---

### POST /api/cart/items
Agrega un producto. Si ya estaba en el carrito, **acumula** la cantidad (no duplica el item).

**Request body:**
```json
{ "productId": "aaaaaaaa-...", "quantity": 2 }
```

**Response 200:** `CartDto` actualizado.

**Errores**: `VALIDATION_ERROR` (400), `PRODUCT_NOT_AVAILABLE` si el producto no existe o no esta
`Active` en este tenant (409).

---

### PUT /api/cart/items/{productId:guid}
Actualiza la cantidad de un item existente. La clave de ruta es el `ProductId`, no un id interno de
`CartItem` — el frontend nunca necesita conocer ese id.

**Request body:**
```json
{ "quantity": 5 }
```

**Response 200:** `CartDto` actualizado.

**Errores**: `NOT_FOUND` si el carrito o el item no existen (404).

---

### DELETE /api/cart/items/{productId:guid}
Quita un item del carrito.

**Response 200:** `CartDto` actualizado.

**Errores**: `NOT_FOUND` si el carrito o el item no existen (404).

---

## Checkout

### POST /api/checkout
Crea el `Order` a partir del carrito actual e inicia el pago. Usa el header `X-Cart-Token`, igual que
el carrito — no hay `cartToken` en el body.

**Auth**: Anónimo

**Request:**
```
Headers: X-Cart-Token: <uuid>
```
```json
{
  "buyerEmail": "comprador@email.com",
  "buyerName": "María García",
  "shippingAddress": "Av. España 1234, Asunción"
}
```
> `shippingAddress` es opcional (nullable).

**Response 200:** `CheckoutResultDto` (ver DTOs de referencia).

**Errores**: `VALIDATION_ERROR` (400, email invalido, carrito vacio), `NOT_FOUND` (404, no hay Store
configurado), `PRODUCT_NOT_AVAILABLE` (409, un item del carrito ya no esta Active),
`CONCURRENCY_CONFLICT` (409, muy raro — solo si se agotan los reintentos de `OrderNumber` bajo
contencion extrema, ver `domain/orders.md`).

---

## Orders — Admin endpoints

### GET /api/orders
Lista pedidos del tenant con paginación SQL.

**Auth**: `OrdersRead`
**Query**: `?page=1&pageSize=20`

**Response 200:** `PagedResult<OrderAdminDto>`.

---

### GET /api/orders/{id:guid}
Detalle de un pedido por ID.

**Auth**: `OrdersRead`
**Response 200**: `OrderAdminDto`
**Errores**: `NOT_FOUND` (404)

---

### PATCH /api/orders/{id:guid}/status
Cambia el estado del pedido manualmente (ej. cancelar, marcar reembolsado).

**Auth**: `OrdersWrite`

**Request body:**
```json
{ "status": 1 }
```
> Valores: 0=PendingPayment, 1=Paid, 2=Cancelled, 3=Refunded. Transiciones validas: ver
> `domain/orders.md`.

**Response 200**: `OrderAdminDto` con nuevo estado.
**Errores**: `NOT_FOUND` (404), `ORDER_INVALID_STATE` (409, transición no permitida)

---

## Payments — Webhook

> `Payment` se crea internamente dentro de `POST /api/checkout` (ver arriba) — no existe un endpoint
> publico para iniciar un pago por separado. Los adapters reales de Bancard/PagoPar siguen sin
> implementar (bloqueados sin su documentacion de API); hoy `IPaymentProviderAdapter` solo tiene
> `FakePaymentProviderAdapter` (dev-only, siempre exitosa, con firma/payload propios — ver
> `domain/payments.md`).

### POST /api/payments/webhooks/{provider}
Recibe y procesa un evento de webhook de pago. Idempotente por `(provider, eventId)`. Excluido de
`TenantResolutionMiddleware` — resuelve el tenant internamente via `(provider, providerPaymentId)`.

**Auth**: Firma/secret propia del provider, validada por `IPaymentProviderAdapter.ValidateWebhookSignature`.

**Request**: body crudo + headers, formato especifico de cada provider (el adapter lo interpreta).
Para `fake` (dev-only): header `X-Fake-Signature` = `FakePaymentProviderAdapter.WebhookSecret`,
body:
```json
{
  "eventId": "evt-123",
  "providerPaymentId": "fake-payment-...",
  "eventType": "Captured"
}
```
> `eventType`: `"Captured"` \| `"Failed"` \| `"Refunded"` (case-sensitive).

**Response 200:** vacio — tanto para un evento nuevo procesado como para uno ya procesado (idempotencia).

**Errores**: `PAYMENT_WEBHOOK_INVALID` (401, firma invalida), `NOT_FOUND` (404, provider no soportado
o `ProviderPaymentId` desconocido), `ORDER_INVALID_STATE` (409, transicion de Payment/Order no
permitida — no deberia pasar en operacion normal, indica un evento fuera de orden).

---

## DTOs de referencia

### CartDto
```csharp
public record CartDto(
    string CartToken, IReadOnlyList<CartItemDto> Items,
    decimal Subtotal, string CurrencyCode
);

public record CartItemDto(
    Guid ProductId, string ProductName, string ProductSlug,
    decimal UnitPrice,  // leido en vivo desde Product, no es un snapshot
    int Quantity, decimal Subtotal
);
```

### ProductAdminDto
```csharp
public record ProductAdminDto(
    Guid Id, string Slug, string? Sku, string Name,
    string? Description, decimal Price, string CurrencyCode,
    ProductStatus Status, int StockOnHand,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc
);
```

### ProductPublicDto
```csharp
public record ProductPublicDto(
    Guid Id, string Slug, string Name,
    string? Description, decimal Price, string CurrencyCode
);
```

### PagedResult<T>
```csharp
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page, int PageSize,
    int TotalCount, int TotalPages
);
```

### TenantOnboardingResultDto
```csharp
public record TenantOnboardingResultDto(Guid TenantId, string Subdomain, string Status);
```

### TenantAdminDto
```csharp
public record TenantAdminDto(
    Guid Id, string Subdomain, string BusinessName,
    string Status, string Plan,
    DateTime CreatedAtUtc, DateTime? ActivatedAtUtc
);
```

### TenantUserDto
```csharp
public record TenantUserDto(
    Guid Id, string Email, string Name,
    string Role, bool IsActive, DateTime CreatedAtUtc
);
```

### StoreProfileDto
```csharp
public record StoreProfileDto(
    Guid StoreId, string Name, string CurrencyCode, string Timezone,
    string? PrimaryColor, string? LogoUrl, string? BackgroundColor, string? Description
);
```

### CheckoutResultDto
```csharp
public record CheckoutResultDto(
    Guid OrderId, int OrderNumber, decimal TotalAmount,
    string CurrencyCode, string PaymentUrl
);
```

### OrderAdminDto
```csharp
public record OrderAdminDto(
    Guid Id, int OrderNumber, string Status,
    string BuyerEmail, string BuyerName, string? ShippingAddress,
    decimal TotalAmount, string CurrencyCode,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc
);

public record OrderItemDto(
    Guid ProductId, string ProductName, string? ProductSku,
    decimal UnitPrice, int Quantity, decimal Subtotal
);
```

## Colección Postman

Todos los endpoints implementados están documentados en:
`Documentation/Postman/EShopy_Backend_MVP.postman_collection.json`

Variables de entorno: `Documentation/Postman/EShopy_Backend_MVP.postman_environment.json`
