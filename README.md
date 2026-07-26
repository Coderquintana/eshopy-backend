# eShopy Backend

Backend multi-tenant para plataforma de e-commerce SaaS (Paraguay). Cada comercio es un tenant
resuelto por subdominio, con su propio catálogo, tienda y usuarios — un único backend, aislamiento
estricto por `TenantId`.

**Stack**: .NET 10 · ASP.NET Core Web API · EF Core 10 · SQL Server · Keycloak (OIDC/RBAC) · xUnit

---

## Arquitectura

Clean Architecture / capas, sin dependencias circulares:

```
EShopy.Domain          Entidades, invariantes, Result<T>. Sin dependencias externas.
EShopy.Application     Casos de uso (Commands/Queries + handlers), puertos (interfaces).
                        Sin referencia a EF Core ni a ningun framework de infraestructura.
EShopy.Infrastructure   EF Core, repositorios, integracion con Keycloak.
EShopy.Api              Controllers, middlewares, composicion (DI), Swagger.
```

Patrones consistentes en todos los modulos: CQRS liviano (un handler por caso de uso), `Result<T>`
para errores de negocio, `DomainException` para invariantes, un repositorio por agregado que
confirma su propia transaccion. Ver [`agents/backend/GOVERNANCE.md`](agents/backend/GOVERNANCE.md)
para las decisiones de arquitectura cerradas y por que se tomaron.

## Modulos

| Modulo | Estado |
|---|---|
| Auth (Keycloak OIDC + RBAC) | ✅ |
| Tenants (onboarding, activacion) | ✅ |
| Store (config de tienda) | ✅ |
| Products (catalogo) | ✅ |
| Subscriptions | ⚠️ Entidad y estados completos, sin integracion de pago real |
| Carts | ❌ Planificado |
| Orders | ❌ Planificado |
| Payments (Bancard / PagoPar) | ❌ Planificado |

Detalle completo por modulo, contratos de API y roadmap: [`agents/backend/`](agents/backend/).

## Quick start

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download), [Docker](https://www.docker.com/).

```bash
# 1. Levantar SQL Server + Keycloak (realm se importa automaticamente)
docker compose up -d

# 2. Aplicar migraciones
dotnet tool restore
dotnet ef database update --project EShopy.Infrastructure --startup-project EShopy.Api

# 3. Correr la API
dotnet run --project EShopy.Api
```

- API: `https://localhost:5001` (Swagger en `/swagger`, ambiente Development)
- Keycloak: `http://localhost:8080` (admin / admin)

Guia completa de Keycloak (roles, usuarios de prueba, troubleshooting de audience):
[`docs/keycloak-setup.md`](docs/keycloak-setup.md).

Coleccion Postman lista para importar: [`Documentation/Postman/`](Documentation/Postman/).

## Tests

```bash
dotnet test
```

70 tests (63 unit + 7 integracion). Los tests de integracion usan test doubles en memoria
(`SecurityWebApplicationFactory`) — no requieren Docker ni una base real corriendo.

## Documentacion

| Que buscas | Donde |
|---|---|
| Estado real del codigo, que falta | [`agents/backend/CURRENT_STATE.md`](agents/backend/CURRENT_STATE.md) |
| Backlog / roadmap por fase | [`agents/backend/BACKLOG.md`](agents/backend/BACKLOG.md) |
| Decisiones de arquitectura cerradas | [`agents/backend/GOVERNANCE.md`](agents/backend/GOVERNANCE.md) |
| Contratos de API (requests/responses) | [`agents/backend/architecture/api-contracts.md`](agents/backend/architecture/api-contracts.md) |
| Esquema de base de datos | [`agents/backend/architecture/database-schema.md`](agents/backend/architecture/database-schema.md) |
| Reglas de dominio por modulo | [`agents/backend/domain/`](agents/backend/domain/) |
| Flujos end-to-end (onboarding, checkout) | [`agents/backend/workflows/`](agents/backend/workflows/) |

## Convenciones

- Codigo en ingles, docs/comentarios en español.
- Commits: [Conventional Commits](https://www.conventionalcommits.org/), chicos y atomicos,
  descripcion en ingles.
- Todo cambio de arquitectura no trivial se documenta en `GOVERNANCE.md` antes de repetirse.

Detalle completo: [`agents/backend/GOVERNANCE.md`](agents/backend/GOVERNANCE.md).
