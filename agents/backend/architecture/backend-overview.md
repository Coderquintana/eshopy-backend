# Architecture — Backend Overview

> Monolito modular .NET 10. Clean Architecture + Vertical Slices. Multi-tenant por subdominio.

## Proyectos de la solución

| Proyecto | Responsabilidad | Dependencias |
|---|---|---|
| `EShopy.Api` | Host HTTP. Controllers thin, middleware, DI, Swagger | Application, Infrastructure |
| `EShopy.Application` | Casos de uso, DTOs, validadores, interfaces de repos | Domain |
| `EShopy.Domain` | Entidades, enums, reglas invariantes, ErrorCodes, Result<T> | ninguna |
| `EShopy.Infrastructure` | EF Core, repos, migraciones, integraciones externas | Application, Domain |
| `EShopy.Tests.Unit` | Tests unitarios de dominio, validadores y handlers | Domain, Application |
| `EShopy.Tests.Integration` | Tests de integración con DB real (Testcontainers/LocalDB) | todos |

## Módulos (bounded contexts)

| Módulo | Carpeta | Estado |
|---|---|---|
| Core | `Common/` | ✅ Implementado |
| Catalog | `Products/` | ⚠️ Parcial (ver `CURRENT_STATE.md`) |
| Tenants | `Tenants/` | ❌ Pendiente Fase 4 |
| Identity | `Identity/` | ⚠️ JWT configurado, UserContext parcial |
| Carts | `Carts/` | ❌ Pendiente Fase 6 |
| Orders | `Orders/` | ❌ Pendiente Fase 7 |
| Payments | `Payments/` | ❌ Pendiente Fase 8 |

## Middleware pipeline (orden en Program.cs)

```
Request
  │
  ├─ CorrelationIdMiddleware      ← agrega/propaga X-Correlation-Id
  ├─ GlobalExceptionMiddleware    ← captura excepciones → ErrorResponse
  ├─ TenantResolutionMiddleware   ← resuelve TenantId desde host/subdominio
  ├─ RequestLoggingScopeMiddleware ← enrichers Serilog (TenantId, UserId, etc.)
  ├─ UseAuthentication()          ← valida JWT Bearer (Keycloak)
  ├─ UseAuthorization()           ← aplica policies RBAC
  │
  └─ Controllers / Minimal APIs
```

## Patrón Result<T>

```csharp
// Ubicación: EShopy.Domain/Common/Result.cs
// Uso en Application layer:
Result<ProductAdminDto> result = await handler.HandleAsync(command, ct);

if (!result.IsSuccess)
    return Problem(result.Error.Message, statusCode: result.Error.HttpStatus);

return Ok(result.Value);
```

> `DomainException` solo para invariantes del dominio (constructores, factory methods).
> Application siempre retorna `Result<T>`. Nunca lanza excepciones de flujo.

## Contextos de request (Scoped)

| Clase | Responsabilidad | Cómo se pobla |
|---|---|---|
| `TenantContext` | TenantId, Subdomain del request actual | `TenantResolutionMiddleware` |
| `UserContext` | UserId, Username, Roles del JWT | `RequestLoggingScopeMiddleware` o filter |

```csharp
// Inyección en controller o service:
public class ProductService(IProductRepository repo, TenantContext tenant) { ... }

// Lectura:
var tenantId = tenant.TenantId ?? throw new InvalidOperationException("Tenant no resuelto");
```

## Autenticación y autorización

```csharp
// Program.cs — Keycloak OIDC
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = config["Auth:Authority"]; // http://localhost:8080/realms/eshopy
        options.Audience  = config["Auth:Audience"];  // eshopy-api
    });

// Policies definidas
options.AddPolicy("CatalogWrite",  p => p.RequireClaim("permissions", "catalog.write"));
options.AddPolicy("OrdersRead",    p => p.RequireClaim("permissions", "orders.read"));
options.AddPolicy("OrdersWrite",   p => p.RequireClaim("permissions", "orders.write"));
options.AddPolicy("UsersManage",   p => p.RequireClaim("permissions", "users.manage"));
// Pendiente: StoreWrite, TenantsRead, PaymentsRead, BillingManage
```

## BaseApiController

```csharp
// EShopy.Api/Controllers/BaseApiController.cs
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected string GetCorrelationId() { ... }
    protected string GetTraceId()       { ... }
    protected string GetUserId()        { ... }
    protected string GetUsername()      { ... }
    protected IEnumerable<string> GetRoles() { ... }
}
```

## ErrorResponse estándar

```json
{
  "traceId": "00-abc123-def456-00",
  "code": "NOT_FOUND",
  "message": "Producto no encontrado.",
  "details": {}
}
```

## Logging estructurado (Serilog)

| Enricher | Fuente | Estado |
|---|---|---|
| `TenantId` | `TenantContext` scoped | ⚠️ Parcial |
| `UserId` | `UserContext` / JWT claim | ⚠️ Parcial |
| `CorrelationId` | Header o generado | ✅ |
| `TraceId` | `Activity.Current.TraceId` | ✅ |
| `RequestPath` | `HttpContext` | ✅ |
| `RequestMethod` | `HttpContext` | ✅ |

## CORS

| Ambiente | Origins permitidos |
|---|---|
| Development | `http://localhost:4200`, `http://localhost:4201` |
| Production | `https://*.eshopy.com.py` |

Headers: `Authorization`, `Content-Type`, `X-Correlation-Id`
Métodos: `GET, POST, PUT, PATCH, DELETE, OPTIONS`

## Observabilidad (estado)

| Herramienta | Estado |
|---|---|
| Serilog (logging) | ✅ Configurado básico |
| OpenTelemetry (traces/métricas) | ❌ Pendiente Fase 9 |
| AuditLog (tabla DB) | ❌ Pendiente Fase 9 |

## Archivos clave

| Archivo | Descripción |
|---|---|
| [EShopy.Api/Program.cs](../../../EShopy.Api/Program.cs) | DI, pipeline, auth, Swagger |
| [EShopy.Api/Controllers/BaseApiController.cs](../../../EShopy.Api/Controllers/BaseApiController.cs) | Base controller con helpers |
| [EShopy.Domain/Common/Result.cs](../../../EShopy.Domain/Common/Result.cs) | Patrón Result<T> |
| [EShopy.Domain/Common/Errors/ErrorCodes.cs](../../../EShopy.Domain/Common/Errors/ErrorCodes.cs) | Códigos de error canónicos |
| [EShopy.Infrastructure/DependencyInjection.cs](../../../EShopy.Infrastructure/DependencyInjection.cs) | DI de infraestructura |
