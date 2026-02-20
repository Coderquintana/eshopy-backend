# Frontend BACKLOG — Kanban

> Estado al 2026-02-19. El frontend no está iniciado. Todo es pendiente.

---

## BLOQUEANTE (backend debe estar listo primero)

| # | Tarea frontend | Requiere backend | Estado backend |
|---|---|---|---|
| BF-01 | Pantalla de gestión de carrito (Admin) | Cart API (Fase 6) | ❌ No implementado |
| BF-02 | Pantalla de pedidos (Admin + Storefront) | Orders API (Fase 7) | ❌ No implementado |
| BF-03 | Flujo de pago (Storefront) | Payments API (Fase 8) | ❌ No implementado |
| BF-04 | Pantalla de suscripción / onboarding | Onboarding API (Fase 4) | ❌ No implementado |

---

## LISTO EN BACKEND — FRONTEND PENDIENTE

Estos endpoints del backend están implementados y el frontend puede construirse:

| # | Tarea frontend | Endpoint backend | Prioridad |
|---|---|---|---|
| LB-01 | Scaffolding: crear workspaces Angular (admin + storefront) | — | Alta |
| LB-02 | `libs/ui`: AppButton, AppTextField, AppSelect, AppToast | — | Alta |
| LB-03 | `libs/ui`: AppDataGrid, AppLoading, AppDialog | — | Alta |
| LB-04 | `libs/ui`: AppPageLayout (Admin sidebar + topbar) | — | Alta |
| LB-05 | Design system: tokens CSS + SCSS variables | — | Alta |
| LB-06 | Admin: AuthInterceptor (JWT Bearer) | Keycloak | Alta |
| LB-07 | Admin: AuthGuard + login redirect | Keycloak | Alta |
| LB-08 | Admin: ErrorInterceptor → AppToast | Todos | Alta |
| LB-09 | Storefront: StoreService → carga tema al iniciar | `GET /api/store` | Alta |
| LB-10 | Admin: ProductListComponent (tabla + paginación) | `GET /api/products` | Media |
| LB-11 | Admin: ProductFormComponent (crear producto) | `POST /api/products` | Media |
| LB-12 | Admin: ProductFormComponent (editar producto) | `PUT /api/products/{id}` | Media |
| LB-13 | Admin: cambio de estado de producto (chip/select) | `PATCH /api/products/{id}/status` | Media |
| LB-14 | Storefront: ProductListComponent (catálogo público) | `GET /api/public/products` | Media |
| LB-15 | Storefront: ProductDetailComponent (slug) | `GET /api/public/products/{slug}` | Media |
| LB-16 | Storefront: AppHeader con logo y nombre del store | `GET /api/store` | Media |

---

## EN PROGRESO

_(nada — frontend no iniciado)_

---

## PROXIMAS (por orden de implementación sugerido)

### Fase F1 — Scaffolding y design system
| # | Tarea | Descripción |
|---|---|---|
| FF1-01 | Crear workspace Angular Nx o multi-project | `apps/admin` + `apps/storefront` + `libs/ui` |
| FF1-02 | Configurar SCSS + variables CSS base | tokens.md como fuente de verdad |
| FF1-03 | Implementar `libs/ui` — componentes base | AppButton, AppTextField, AppSelect |
| FF1-04 | Implementar `libs/ui` — layout y data | AppDataGrid, AppPageLayout, AppLoading, AppToast |
| FF1-05 | Configurar HttpClient + interceptors base | AuthInterceptor, ErrorInterceptor, CorrelationIdInterceptor |

### Fase F2 — Autenticación Admin
| # | Tarea | Descripción |
|---|---|---|
| FF2-01 | Integrar Keycloak JS en Admin | `keycloak-angular` o adapter directo |
| FF2-02 | AuthGuard para rutas protegidas | Redirige a Keycloak si no autenticado |
| FF2-03 | Extraer permisos del JWT | `catalog.write`, `orders.read`, etc. |
| FF2-04 | PermissionGuard por ruta | Ocultar/bloquear según permisos |

### Fase F3 — Storefront base
| # | Tarea | Descripción |
|---|---|---|
| FF3-01 | AppHeader: logo, nombre, link al carrito | `GET /api/store` |
| FF3-02 | AppFooter: info básica del store | — |
| FF3-03 | StoreService: carga tema y lo inyecta en `:root` | `GET /api/store` |
| FF3-04 | CartService: genera/persiste CartToken en localStorage | Local |

### Fase F4 — Catálogo Admin
| # | Tarea | Descripción |
|---|---|---|
| FF4-01 | ProductListComponent: tabla + paginación + filtros | `GET /api/products` |
| FF4-02 | ProductFormComponent: crear (draft) | `POST /api/products` |
| FF4-03 | ProductFormComponent: editar | `PUT /api/products/{id}` |
| FF4-04 | ProductStatusChip: cambiar estado con confirmación | `PATCH /api/products/{id}/status` |

### Fase F5 — Catálogo Storefront
| # | Tarea | Descripción |
|---|---|---|
| FF5-01 | ProductGridComponent: cards con paginación | `GET /api/public/products` |
| FF5-02 | ProductDetailComponent: detail page por slug | `GET /api/public/products/{slug}` |
| FF5-03 | AddToCartButton: agrega al carrito | `POST /api/cart/items` |

### Fase F6 — Carrito (requiere backend Fase 6)
| # | Tarea | Descripción |
|---|---|---|
| FF6-01 | CartComponent: lista items + totales | `GET /api/cart` |
| FF6-02 | CartItemActions: +/- cantidad, eliminar | `PUT/DELETE /api/cart/items/{id}` |
| FF6-03 | CartBadge en header: cantidad de items | Local + `GET /api/cart` |

### Fase F7 — Checkout (requiere backend Fase 7)
| # | Tarea | Descripción |
|---|---|---|
| FF7-01 | CheckoutForm: datos del comprador | `POST /api/checkout` |
| FF7-02 | OrderConfirmationComponent: pantalla de espera | Polling `GET /api/orders/{id}` |
| FF7-03 | PaymentReturnComponent: success/failed | `GET /api/orders/{id}` |
| FF7-04 | OrderListComponent (Admin): tabla de pedidos | `GET /api/orders` |
| FF7-05 | OrderDetailComponent (Admin): detalle | `GET /api/orders/{id}` |

---

## COMPLETADAS

_(nada — frontend no iniciado)_
