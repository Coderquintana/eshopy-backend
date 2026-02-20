# BACKLOG — Kanban eShopy Backend

> Estado al 2026-02-19. Actualizar este archivo al iniciar/completar tareas.

---

## BLOQUEANTE (debe resolverse antes de continuar)

| # | Tarea | Módulo | Detalle |
|---|---|---|---|
| B-01 | Refactor ProductService a Result<T> | Catalog | Service lanza excepciones; debe retornar `Result<T>` |
| B-02 | Agregar StoreId a Product.cs | Catalog | FK obligatorio según GOVERNANCE. Requiere migración EF |
| B-03 | Paginación en SQL | Catalog | `GetAdminListAsync` y `GetPublicListAsync` cargan todo en memoria |

---

## EN PROGRESO

| # | Tarea | Módulo | Fase |
|---|---|---|---|
| P-01 | EF Core integration (EfProductRepository) | Catalog | Fase 3 — implementado pero paginación pendiente |

---

## PROXIMAS (ordenadas por prioridad)

### Fase 2 — Seguridad
| # | Tarea | Descripción |
|---|---|---|
| F2-01 | [Authorize] en todos los endpoints admin | StoreController y futuros módulos necesitan policy |
| F2-02 | CORS por ambiente | Dev: localhost:4200/4201 / Prod: *.eshopy.com.py |
| F2-03 | UserContext completo | Mapear claims de Keycloak a `UserContext` |

### Fase 3 — Persistencia base
| # | Tarea | Descripción |
|---|---|---|
| F3-01 | Global Query Filter por TenantId | EF Core: filtro automático en todas las entidades multi-tenant |
| F3-02 | Interceptor TenantId + fechas UTC | Impedir SaveChanges si TenantId ausente; setear timestamps UTC |
| F3-03 | InMemoryTenantResolver configurable | Permitir tenant configurable en dev (no solo 'localhost') |
| F3-04 | Migración completa con todas las tablas | Orders, Payments, Carts, Tenants, Subscriptions |

### Fase 4 — Tenants (Onboarding)
| # | Tarea | Descripción |
|---|---|---|
| F4-01 | TenantEntity + StoreEntity | Entidades de dominio con estados |
| F4-02 | CreateTenant con Result<T> | Caso de uso completo |
| F4-03 | Integración Keycloak para Owner | Crear usuario en realm al crear tenant |
| F4-04 | SubscriptionEntity | Estado, plan, fechas |

### Fase 5 — Catalog (refactor)
| # | Tarea | Descripción |
|---|---|---|
| F5-01 | Commands separados de Queries | Separar ProductService en Command/Query handlers |
| F5-02 | CurrencyCode desde Store | Eliminar "PYG" hardcodeado; tomar del Store del tenant |
| F5-03 | Auditoria de cambios (precio/estado) | AuditLog en tabla DB |
| F5-04 | ProductImages (metadata imágenes) | Entidad + endpoint de imágenes |

### Fase 6 — Carrito
| # | Tarea | Descripción |
|---|---|---|
| F6-01 | CartEntity + CartItemEntity | Persistencia server-side con CartToken |
| F6-02 | Commands: Add/Update/Remove CartItem | CRUD de items del carrito |
| F6-03 | Query: GetCart por CartToken | Obtener carrito con items |
| F6-04 | Job limpieza carritos expirados | Background job periódico |

### Fase 7 — Pedidos
| # | Tarea | Descripción |
|---|---|---|
| F7-01 | OrderEntity + OrderItemEntity | Snapshot de precio en item |
| F7-02 | Checkout con Result<T> | Caso de uso completo |
| F7-03 | OrderNumber con TenantCounters | Secuencial por tenant, UPDLOCK/ROWLOCK |
| F7-04 | Transiciones OrderStatus | Controladas según tabla de dominio |

### Fase 8 — Pagos
| # | Tarea | Descripción |
|---|---|---|
| F8-01 | PaymentEntity + IPaymentProviderAdapter | Contrato de adaptador |
| F8-02 | BancardAdapter | Integración con Bancard API |
| F8-03 | PagoParAdapter | Integración con PagoPar API |
| F8-04 | Webhook endpoint idempotente | `POST /api/payments/webhooks/{provider}` |
| F8-05 | Validación de firma/secret webhook | Por provider |

### Fase 9 — Observabilidad
| # | Tarea | Descripción |
|---|---|---|
| F9-01 | Enrichers Serilog completos | TenantId, UserId, CorrelationId, TraceId |
| F9-02 | OpenTelemetry traces y métricas | Instrumentación básica |
| F9-03 | AuditLog en operaciones sensibles | Tabla AuditLogs con eventos listados en GOVERNANCE |

### Fase 10 — Testing
| # | Tarea | Descripción |
|---|---|---|
| F10-01 | Tests unitarios: validadores, dominio, handlers | xUnit + FluentAssertions + NSubstitute |
| F10-02 | Tests integración: Testcontainers | Aislamiento multi-tenant por tenant |
| F10-03 | Tests webhooks | Idempotencia + firma inválida |
| F10-04 | Tests RBAC | Políticas, endpoints sin token → 401 |

---

## COMPLETADAS

| # | Tarea | Módulo | Fecha |
|---|---|---|---|
| C-01 | Estructura de proyectos y solución | Core | 2026-02-05 |
| C-02 | Swagger + XML docs | Core | 2026-02-05 |
| C-03 | TenantContext + TenantResolutionMiddleware | Core | 2026-02-07 |
| C-04 | CorrelationIdMiddleware | Core | 2026-02-07 |
| C-05 | GlobalExceptionMiddleware | Core | 2026-02-07 |
| C-06 | BaseApiController | Core | 2026-02-07 |
| C-07 | ErrorResponse estándar | Core | 2026-02-07 |
| C-08 | Product aggregate (dominio) | Catalog | 2026-02-14 |
| C-09 | ProductStatus enum y transiciones | Catalog | 2026-02-14 |
| C-10 | IProductRepository + IProductService | Catalog | 2026-02-14 |
| C-11 | ProductService (monolítico, pendiente refactor) | Catalog | 2026-02-14 |
| C-12 | Admin + Public ProductsController | Catalog | 2026-02-14 |
| C-13 | FluentValidation para Products | Catalog | 2026-02-14 |
| C-14 | AppEntity base con auditoría | Core | 2026-02-07 |
| C-15 | EfProductRepository + migraciones iniciales | Catalog | 2026-02-07 |
| C-16 | Result<T> en dominio | Core | 2026-02-07 |
| C-17 | JWT Bearer (Keycloak) configurado | Auth | 2026-02-14 |
| C-18 | Authorization policies (CatalogWrite, etc.) | Auth | 2026-02-14 |
| C-19 | Colección Postman MVP | Docs | 2026-02-07 |
| C-20 | documentation.md consolidado v2.0 | Docs | 2026-02-17 |
