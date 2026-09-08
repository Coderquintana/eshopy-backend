# eShopy Backend — Documentación para Agentes IA

> Índice maestro. Lee esta tabla primero para saber qué archivo abrir.

El frontend (Angular, Admin + Storefront) vive en un repo aparte,
`eshopy-frontend`, con su propia carpeta `agents/`. Lo que había acá antes
(`agents/frontend/`) era el plan escrito antes de que ese repo existiera —
se borró porque quedaba desactualizado en silencio; la documentación viva
del frontend está solo en `eshopy-frontend/agents/`.

## Navegación por tarea

| Si tu tarea es... | Lee |
|---|---|
| Empezar desde cero | [`backend/CURRENT_STATE.md`](backend/CURRENT_STATE.md) |
| Ver qué hay que hacer | [`backend/BACKLOG.md`](backend/BACKLOG.md) |
| Entender decisiones firmes de arquitectura | [`backend/GOVERNANCE.md`](backend/GOVERNANCE.md) |
| Trabajar en una entidad de dominio | [`backend/domain/<entidad>.md`](backend/domain/) |
| Diseñar o consumir un endpoint | [`backend/architecture/api-contracts.md`](backend/architecture/api-contracts.md) |
| Flujo de checkout | [`backend/workflows/checkout-flow.md`](backend/workflows/checkout-flow.md) |
| Onboarding de tenant | [`backend/workflows/onboarding-flow.md`](backend/workflows/onboarding-flow.md) |
| Ciclo de vida de productos | [`backend/workflows/product-lifecycle.md`](backend/workflows/product-lifecycle.md) |
| Escribir o revisar tests | [`backend/testing/test-strategy.md`](backend/testing/test-strategy.md) |
| Esquema de base de datos | [`backend/architecture/database-schema.md`](backend/architecture/database-schema.md) |
| Levantar el entorno local | [`README.md`](../README.md) del repo — "Quick start" |

---

## Estructura completa

```
agents/
├── README.md                          ← este archivo (índice maestro)
└── backend/                           ← .NET 10 / ASP.NET Core / EF Core
    ├── GOVERNANCE.md                  ← decisiones técnicas firmes
    ├── BACKLOG.md                     ← kanban backend
    ├── CURRENT_STATE.md               ← estado actual del código .NET
    ├── domain/
    │   ├── products.md
    │   ├── orders.md
    │   ├── payments.md
    │   ├── tenants.md
    │   └── subscriptions.md
    ├── architecture/
    │   ├── backend-overview.md        ← capas, middleware, patrones, auth
    │   ├── database-schema.md         ← tablas, índices, AppEntity base
    │   └── api-contracts.md           ← endpoints, DTOs, códigos de error
    ├── workflows/
    │   ├── product-lifecycle.md       ← Draft → Active → Archived
    │   ├── onboarding-flow.md         ← creación y activación de tenant
    │   └── checkout-flow.md           ← carrito → pedido → pago (backend)
    └── testing/
        ├── test-strategy.md           ← pirámide, herramientas, convenciones
        └── critical-test-cases.md     ← casos Given/When/Then obligatorios
```

---

## Reglas para agentes IA

1. Leer `backend/GOVERNANCE.md` antes de proponer cambios estructurales.
2. Verificar `backend/CURRENT_STATE.md` antes de asumir que algo está implementado.
3. **TenantId nunca al frontend** — se resuelve en el middleware del backend.
4. Código en inglés, docs/comentarios en español.
5. Todo cambio en endpoints → actualizar `Documentation/Postman/`.
6. Columnas EF Core → siempre `HasComment()` en la configuración.
7. Commits: Conventional Commits, descripción en inglés.

---

## Archivos del repo relacionados

| Archivo | Propósito |
|---|---|
| `documentation.md` | Compilado v2.0 (2026-02-17) de 11 documentos originales — **desactualizado**: es anterior a Tenants, Carts, Orders, Payments y todo el Admin. Sirve como narrativa histórica del plan inicial, no como estado actual — para eso, `backend/CURRENT_STATE.md` |
| `Documentation Copy/` | Los 11 documentos originales (`.docx`) de los que sale `documentation.md` |
| `Documentation/Postman/` | Colección Postman — contrato de endpoints MVP |
| `Documentation/Keycloak/` | Configuración del realm Keycloak para dev |
| `docs/keycloak-setup.md` | Guía de Keycloak (roles, usuarios de prueba, troubleshooting) |

---

## Stack

| Capa | Tecnología |
|---|---|
| Backend | .NET 10 / ASP.NET Core |
| ORM | Entity Framework Core 10 |
| Base de datos | SQL Server (Docker Compose en dev, ver `docker-compose.yml`) |
| Auth | Keycloak 24+ (OIDC/JWT) |
| Validación | FluentValidation 11+ |
| Tests | xUnit |
