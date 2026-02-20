# eShopy — Documentación para Agentes IA

> Índice maestro. Lee esta tabla primero para saber qué carpeta abrir.

## ¿Estás trabajando en...?

| Tarea | Carpeta | Archivo de entrada |
|---|---|---|
| Backend (.NET / API) | [`backend/`](backend/) | [`backend/CURRENT_STATE.md`](backend/CURRENT_STATE.md) |
| Frontend (Angular) | [`frontend/`](frontend/) | [`frontend/CURRENT_STATE.md`](frontend/CURRENT_STATE.md) |

---

## Navegación por tarea

| Si tu tarea es... | Lee |
|---|---|
| Ver qué hay que hacer (backend) | [`backend/BACKLOG.md`](backend/BACKLOG.md) |
| Ver qué hay que hacer (frontend) | [`frontend/BACKLOG.md`](frontend/BACKLOG.md) |
| Entender decisiones firmes de arquitectura | [`backend/GOVERNANCE.md`](backend/GOVERNANCE.md) |
| Entender reglas de código frontend | [`frontend/GOVERNANCE.md`](frontend/GOVERNANCE.md) |
| Trabajar en una entidad de dominio | [`backend/domain/<entidad>.md`](backend/domain/) |
| Diseñar o consumir un endpoint | [`backend/architecture/api-contracts.md`](backend/architecture/api-contracts.md) |
| Implementar pantalla en Admin o Storefront | [`frontend/apps/admin.md`](frontend/apps/admin.md) o [`frontend/apps/storefront.md`](frontend/apps/storefront.md) |
| Integrar un endpoint en el frontend | [`frontend/api-contracts/<módulo>.md`](frontend/api-contracts/) |
| Crear o usar un componente de UI | [`frontend/design-system/components.md`](frontend/design-system/components.md) |
| Flujo de checkout (backend) | [`backend/workflows/checkout-flow.md`](backend/workflows/checkout-flow.md) |
| Flujo de checkout (frontend) | [`frontend/workflows/checkout-flow.md`](frontend/workflows/checkout-flow.md) |
| Onboarding de tenant | [`backend/workflows/onboarding-flow.md`](backend/workflows/onboarding-flow.md) |
| Ciclo de vida de productos | [`backend/workflows/product-lifecycle.md`](backend/workflows/product-lifecycle.md) |
| Escribir o revisar tests | [`backend/testing/test-strategy.md`](backend/testing/test-strategy.md) |
| Esquema de base de datos | [`backend/architecture/database-schema.md`](backend/architecture/database-schema.md) |

---

## Estructura completa

```
agents/
├── README.md                          ← este archivo (índice maestro)
│
├── backend/                           ← .NET 10 / ASP.NET Core / EF Core
│   ├── GOVERNANCE.md                  ← decisiones técnicas firmes
│   ├── BACKLOG.md                     ← kanban backend
│   ├── CURRENT_STATE.md               ← estado actual del código .NET
│   ├── domain/
│   │   ├── products.md
│   │   ├── orders.md
│   │   ├── payments.md
│   │   ├── tenants.md
│   │   └── subscriptions.md
│   ├── architecture/
│   │   ├── backend-overview.md        ← capas, middleware, patrones, auth
│   │   ├── database-schema.md         ← tablas, índices, AppEntity base
│   │   └── api-contracts.md           ← endpoints, DTOs, códigos de error
│   ├── workflows/
│   │   ├── product-lifecycle.md       ← Draft → Active → Archived
│   │   ├── onboarding-flow.md         ← creación y activación de tenant
│   │   └── checkout-flow.md           ← carrito → pedido → pago (backend)
│   └── testing/
│       ├── test-strategy.md           ← pirámide, herramientas, convenciones
│       └── critical-test-cases.md     ← casos Given/When/Then obligatorios
│
└── frontend/                          ← Angular 18+ (Admin + Storefront)
    ├── README.md                      ← índice frontend
    ├── GOVERNANCE.md                  ← reglas de código, validaciones, stack
    ├── BACKLOG.md                     ← kanban frontend
    ├── CURRENT_STATE.md               ← estado actual (todo pendiente)
    ├── design-system/
    │   ├── tokens.md                  ← colores, tipografía, spacing, radius
    │   ├── components.md              ← AppButton, AppTextField, AppDataGrid…
    │   └── layouts.md                 ← Admin (sidebar) y Storefront (header/footer)
    ├── apps/
    │   ├── admin.md                   ← routing, auth Keycloak, features Admin
    │   └── storefront.md              ← routing, StoreService, CartService
    ├── api-contracts/
    │   ├── products.md                ← interfaces TS + validaciones UX + errores
    │   ├── cart.md
    │   ├── orders.md
    │   └── payments.md
    └── workflows/
        ├── product-management.md      ← CRUD productos (código TS completo)
        ├── checkout-flow.md           ← catálogo → carrito → pago (frontend)
        └── error-handling.md         ← ErrorInterceptor + mapeo código → mensaje
```

---

## Relación entre backend y frontend

| Archivo backend | Archivo frontend que lo consume |
|---|---|
| `backend/architecture/api-contracts.md` | `frontend/api-contracts/*.md` |
| `backend/domain/products.md` | `frontend/api-contracts/products.md` |
| `backend/domain/orders.md` | `frontend/api-contracts/orders.md` |
| `backend/domain/payments.md` | `frontend/api-contracts/payments.md` |
| `backend/workflows/checkout-flow.md` | `frontend/workflows/checkout-flow.md` |
| `backend/GOVERNANCE.md` | `frontend/GOVERNANCE.md` §validaciones |

---

## Reglas para agentes IA

1. Leer `backend/GOVERNANCE.md` antes de proponer cambios estructurales.
2. Verificar `backend/CURRENT_STATE.md` antes de asumir que algo está implementado.
3. **TenantId nunca al frontend** — se resuelve en el middleware del backend.
4. Código en inglés, docs/comentarios en español.
5. Todo cambio en endpoints → actualizar `Documentation/Postman/`.
6. Columnas EF Core → siempre `HasComment()` en la configuración.
7. Commits: `type(scope): resumen corto`.

---

## Archivos del repo relacionados

| Archivo | Propósito |
|---|---|
| `TASKS.md` | Contexto operativo general (convenciones, encoding, historial) |
| `documentation.md` | Compilado v2.0 de 11 documentos — fuente de verdad para humanos |
| `Documentation/Postman/` | Colección Postman — contrato de endpoints MVP |
| `Documentation/Keycloak/` | Configuración del realm Keycloak para dev |

---

## Stack

| Capa | Tecnología |
|---|---|
| Backend | .NET 10 / ASP.NET Core |
| ORM | Entity Framework Core 10 |
| Base de datos | SQL Server (`localhost\SQLEXPRESS` en dev) |
| Auth | Keycloak 24+ (OIDC/JWT) |
| Validación | FluentValidation 11+ |
| Frontend | Angular 18+ standalone |
| Tests | xUnit + Testcontainers |
