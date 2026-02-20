# eShopy Frontend — Documentación para Agentes IA

> Punto de entrada para tareas de frontend. Lee esta tabla primero.

## Navegación rápida

| Si tu tarea es... | Lee primero | Luego |
|---|---|---|
| Empezar desde cero | Este archivo + `CURRENT_STATE.md` | `GOVERNANCE.md` |
| Construir una pantalla del Admin | `apps/admin.md` | módulo relevante en `api-contracts/` |
| Construir una pantalla del Storefront | `apps/storefront.md` | `workflows/checkout-flow.md` |
| Crear o usar un componente UI | `design-system/components.md` | `design-system/tokens.md` |
| Definir el layout de una página | `design-system/layouts.md` | — |
| Integrar un endpoint del backend | `api-contracts/<módulo>.md` | `GOVERNANCE.md` §validaciones |
| Gestionar productos (Admin) | `workflows/product-management.md` | `api-contracts/products.md` |
| Implementar el flujo de checkout | `workflows/checkout-flow.md` | `api-contracts/cart.md` + `api-contracts/payments.md` |
| Manejar errores del backend | `workflows/error-handling.md` | `GOVERNANCE.md` §errores |
| Ver qué tareas están pendientes | `BACKLOG.md` | `CURRENT_STATE.md` |
| Entender reglas no negociables | `GOVERNANCE.md` | — |
| Sincronizar contratos con backend | `../architecture/api-contracts.md` | `api-contracts/<módulo>.md` |

---

## Estructura de este directorio

```
agents/frontend/
├── README.md                    ← este archivo
├── GOVERNANCE.md                ← reglas no negociables del frontend
├── BACKLOG.md                   ← tareas frontend por estado
├── CURRENT_STATE.md             ← estado actual (todo pendiente)
├── design-system/
│   ├── tokens.md                ← colores, tipografía, spacing, radius, sombras
│   ├── components.md            ← specs AppButton, AppTextField, etc.
│   └── layouts.md               ← estructura Admin (sidebar) y Storefront (header/footer)
├── apps/
│   ├── admin.md                 ← routing, features, auth del Admin Panel
│   └── storefront.md            ← routing, features, theming del Storefront
├── api-contracts/
│   ├── products.md              ← interfaces TS + validaciones UX + errores
│   ├── cart.md
│   ├── orders.md
│   └── payments.md
└── workflows/
    ├── checkout-flow.md         ← flujo carrito→pago con código TS
    ├── product-management.md    ← CRUD productos desde Admin con código TS
    └── error-handling.md        ← mapeo ErrorResponse → mensajes amigables
```

---

## Relación con el backend

| Fuente backend | Uso en frontend |
|---|---|
| `../architecture/api-contracts.md` | Fuente de verdad de endpoints. Los `api-contracts/*.md` de frontend amplían esto con TypeScript y UX |
| `../GOVERNANCE.md` | Decisiones de arquitectura backend que el frontend debe respetar |
| `../domain/products.md` | Estados y transiciones que el frontend muestra (nunca valida) |
| `../workflows/checkout-flow.md` | Flujo backend de checkout — el frontend implementa la contraparte visual |

---

## Stack del frontend

| Capa | Tecnología | Notas |
|---|---|---|
| Framework | Angular 18+ standalone | Sin NgModules — standalone components |
| Estilos | CSS variables + SCSS | Sin frameworks CSS externos |
| Forms | Reactive Forms | `FormBuilder`, `Validators` |
| Estado | Signals (`signal`, `computed`, `effect`) | Sin NgRx en MVP |
| HTTP | `HttpClient` + interceptors | AuthInterceptor + ErrorInterceptor |
| Auth | Keycloak JS adapter | Solo en Admin |
| Testing | Jest + Angular Testing Library | Por definir |

---

## Reglas de actualización de este directorio

1. Al agregar un endpoint nuevo en backend → actualizar `api-contracts/<módulo>.md`.
2. Al agregar un componente nuevo en `libs/ui` → actualizar `design-system/components.md`.
3. Al completar una pantalla → actualizar `BACKLOG.md` y `CURRENT_STATE.md`.
4. Al cambiar un token de diseño → actualizar `design-system/tokens.md`.
