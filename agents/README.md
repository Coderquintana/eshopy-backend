# eShopy — Documentación para Agentes IA

> Punto de entrada. Lee esta tabla primero para saber qué archivo leer según tu tarea.

## Navegación rápida

| Si tu tarea es... | Lee primero | Luego |
|---|---|---|
| Entender el proyecto desde cero | Este archivo + `CURRENT_STATE.md` | `architecture/backend-overview.md` |
| Agregar o modificar una entidad de dominio | `domain/<entidad>.md` | `architecture/database-schema.md` |
| Agregar un endpoint o DTO | `architecture/api-contracts.md` | `domain/<entidad>.md` |
| Implementar un caso de uso | `workflows/<flujo>.md` | `domain/<entidad>.md` |
| Hacer un refactor o corregir deuda técnica | `CURRENT_STATE.md` + `GOVERNANCE.md` | código relevante |
| Ver qué tareas están pendientes | `BACKLOG.md` | `CURRENT_STATE.md` |
| Entender decisiones técnicas firmes | `GOVERNANCE.md` | — |
| Escribir o revisar tests | `testing/test-strategy.md` | `testing/critical-test-cases.md` |
| Implementar Products | `domain/products.md` | `workflows/product-lifecycle.md` |
| Implementar checkout | `workflows/checkout-flow.md` | `domain/orders.md` + `domain/payments.md` |
| Implementar onboarding de tenant | `workflows/onboarding-flow.md` | `domain/tenants.md` + `domain/subscriptions.md` |
| Trabajar en frontend | `architecture/frontend-overview.md` | `architecture/api-contracts.md` |

---

## Estructura de este directorio

```
agents/
├── README.md                    ← este archivo
├── GOVERNANCE.md                ← reglas no negociables
├── BACKLOG.md                   ← tareas por estado
├── CURRENT_STATE.md             ← estado actual del código
├── domain/
│   ├── products.md
│   ├── orders.md
│   ├── payments.md
│   ├── tenants.md
│   └── subscriptions.md
├── architecture/
│   ├── backend-overview.md
│   ├── frontend-overview.md
│   ├── database-schema.md
│   └── api-contracts.md
├── workflows/
│   ├── onboarding-flow.md
│   ├── checkout-flow.md
│   └── product-lifecycle.md
└── testing/
    ├── test-strategy.md
    └── critical-test-cases.md
```

---

## Reglas para agentes IA

1. **Leer `GOVERNANCE.md` antes de proponer cambios estructurales** — contiene decisiones firmes.
2. **Verificar `CURRENT_STATE.md`** antes de asumir que algo está implementado.
3. **No exponer TenantId al frontend** — se resuelve siempre en el middleware del backend.
4. **Mantener convenciones existentes**: nombres en inglés para código, comentarios/docs en español.
5. **Todo cambio en endpoints** → actualizar colección Postman en `Documentation/Postman/`.
6. **Columnas EF Core** → siempre incluir `HasComment()` en la configuración.
7. **Commits pequeños y temáticos**: `type(scope): resumen corto`.

---

## Relación con otros archivos del repo

| Archivo | Propósito |
|---|---|
| `TASKS.md` | Contexto operativo general para IAs (convenciones, encoding, historial) |
| `documentation.md` | Compilado v2.0 de 11 documentos — fuente de verdad para humanos |
| `Documentation/Postman/` | Colección Postman — contrato de endpoints MVP |
| `Documentation/Keycloak/` | Configuración del realm Keycloak para dev |

---

## Stack resumido

| Capa | Tecnología |
|---|---|
| Backend | .NET 10 / ASP.NET Core |
| ORM | Entity Framework Core 10 |
| Base de datos | SQL Server (dev: `localhost\SQLEXPRESS`) |
| Auth | Keycloak 24+ (OIDC/JWT) |
| Validación | FluentValidation 11+ |
| Logging | Serilog 4+ |
| Frontend | Angular 18+ |
| Tests | xUnit + Testcontainers |
