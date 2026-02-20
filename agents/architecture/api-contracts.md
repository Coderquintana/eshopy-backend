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
| `SUBDOMAIN_ALREADY_EXISTS` | 409 | Subdominio ya en uso al crear tenant |
| `UNAUTHORIZED` | 401 | Token ausente o inválido |
| `FORBIDDEN` | 403 | Sin permisos (token válido pero policy falla) |
| `NOT_FOUND` | 404 | Recurso no encontrado |
| `CONFLICT` | 409 | Slug/SKU duplicado u otro conflicto |
| `PRODUCT_NOT_AVAILABLE` | 409 | Producto no disponible para agregar al carrito |
| `PRODUCT_INVALID_STATE` | 409 | Transición de estado de producto no permitida |
| `ORDER_INVALID_STATE` | 409 | Transición de orden no permitida |
| `PAYMENT_WEBHOOK_INVALID` | 401 | Webhook con firma inválida |
| `PAYMENT_PROVIDER_ERROR` | 502 | Error al comunicarse con el provider de pago |
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

### GET /api/store
Configuración pública del store.

**Auth**: Anónimo

**Response 200:**
```json
{
  "storeId": "22222222-...",
  "name": "Mi Tienda",
  "currencyCode": "PYG",
  "timezone": "America/Asuncion",
  "primaryColor": "#007bff",
  "logoUrl": "https://..."
}
```

---

## Cart endpoints (diseño, no implementado)

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/cart/items` | Anónimo | Agregar item al carrito |
| PUT | `/api/cart/items/{id}` | Anónimo | Actualizar cantidad |
| DELETE | `/api/cart/items/{id}` | Anónimo | Eliminar item |
| GET | `/api/cart` | Anónimo | Obtener carrito por `CartToken` header |

---

## Orders endpoints (diseño, no implementado)

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/checkout` | Anónimo | Crear Order desde carrito |
| GET | `/api/orders` | OrdersRead | Listar pedidos (admin) |
| GET | `/api/orders/{id}` | OrdersRead | Detalle de pedido |
| PATCH | `/api/orders/{id}/status` | OrdersWrite | Actualizar estado |

---

## Payments endpoints (diseño, no implementado)

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/payments` | — | Iniciar pago (retorna `paymentUrl`) |
| POST | `/api/payments/webhooks/{provider}` | Firma provider | Webhook idempotente |

---

## Onboarding endpoints (diseño, no implementado)

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/onboarding/tenants` | Público | Crear tenant (PendingPayment) |

---

## DTOs de referencia

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

## Colección Postman

Todos los endpoints implementados están documentados en:
`Documentation/Postman/EShopy_Backend_MVP.postman_collection.json`

Variables de entorno: `Documentation/Postman/EShopy_Backend_MVP.postman_environment.json`
