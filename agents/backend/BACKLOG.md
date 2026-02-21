# BACKLOG - Kanban eShopy Backend

> Estado al 2026-02-21. Actualizar este archivo al iniciar/completar tareas.

---

## BLOQUEANTE (debe resolverse antes de continuar)

| # | Tarea | Modulo | Detalle |
|---|---|---|---|
| B-01 | Refactor ProductService a Result<T> | Catalog | Service lanza excepciones; debe retornar `Result<T>` |
| B-02 | Agregar StoreId a Product.cs | Catalog | FK obligatorio segun GOVERNANCE. Requiere migracion EF |
| B-03 | Paginacion en SQL | Catalog | `GetAdminListAsync` y `GetPublicListAsync` cargan todo en memoria |

---

## EN PROGRESO

| # | Tarea | Modulo | Fase |
|---|---|---|---|
| P-01 | EF Core integration (EfProductRepository) | Catalog | Fase 3 - implementado parcialmente |

---

## PROXIMAS (ordenadas por prioridad)

### Fase 3 - Persistencia base
| # | Tarea | Descripcion |
|---|---|---|
| F3-01 | Global Query Filter por TenantId | EF Core: filtro automatico en entidades multi-tenant |
| F3-02 | Interceptor TenantId + fechas UTC | Impedir SaveChanges si TenantId ausente; setear timestamps UTC |
| F3-03 | InMemoryTenantResolver configurable | Permitir tenant configurable en dev (no solo `localhost`) |
| F3-04 | Migracion completa con todas las tablas | Orders, Payments, Carts, Tenants, Subscriptions |

### Fase 4 - Tenants (Onboarding)
| # | Tarea | Descripcion |
|---|---|---|
| F4-01 | TenantEntity + StoreEntity | Entidades de dominio con estados |
| F4-02 | CreateTenant con Result<T> | Caso de uso completo |
| F4-03 | Integracion Keycloak para Owner | Crear usuario en realm al crear tenant |
| F4-04 | SubscriptionEntity | Estado, plan, fechas |

### Fase 5 - Catalog (refactor)
| # | Tarea | Descripcion |
|---|---|---|
| F5-01 | Commands separados de Queries | Separar ProductService en Command/Query handlers |
| F5-02 | CurrencyCode desde Store | Eliminar "PYG" hardcodeado |
| F5-03 | Auditoria de cambios (precio/estado) | AuditLog en tabla DB |
| F5-04 | ProductImages (metadata imagenes) | Entidad + endpoint de imagenes |

### Fase 6 - Carrito
| # | Tarea | Descripcion |
|---|---|---|
| F6-01 | CartEntity + CartItemEntity | Persistencia server-side con CartToken |
| F6-02 | Commands: Add/Update/Remove CartItem | CRUD de items del carrito |
| F6-03 | Query: GetCart por CartToken | Obtener carrito con items |
| F6-04 | Job limpieza carritos expirados | Background job periodico |

### Fase 7 - Pedidos
| # | Tarea | Descripcion |
|---|---|---|
| F7-01 | OrderEntity + OrderItemEntity | Snapshot de precio en item |
| F7-02 | Checkout con Result<T> | Caso de uso completo |
| F7-03 | OrderNumber con TenantCounters | Secuencial por tenant, UPDLOCK/ROWLOCK |
| F7-04 | Transiciones OrderStatus | Controladas segun tabla de dominio |

### Fase 8 - Pagos
| # | Tarea | Descripcion |
|---|---|---|
| F8-01 | PaymentEntity + IPaymentProviderAdapter | Contrato de adaptador |
| F8-02 | BancardAdapter | Integracion con Bancard API |
| F8-03 | PagoParAdapter | Integracion con PagoPar API |
| F8-04 | Webhook endpoint idempotente | `POST /api/payments/webhooks/{provider}` |
| F8-05 | Validacion de firma/secret webhook | Por provider |

### Fase 9 - Observabilidad
| # | Tarea | Descripcion |
|---|---|---|
| F9-01 | Enrichers Serilog completos | TenantId, UserId, CorrelationId, TraceId |
| F9-02 | OpenTelemetry traces y metricas | Instrumentacion basica |
| F9-03 | AuditLog en operaciones sensibles | Tabla AuditLogs |

### Fase 10 - Testing
| # | Tarea | Descripcion |
|---|---|---|
| F10-01 | Tests unitarios: validadores, dominio, handlers | xUnit + FluentAssertions + NSubstitute |
| F10-02 | Tests integracion: Testcontainers | Aislamiento multi-tenant por tenant |
| F10-03 | Tests webhooks | Idempotencia + firma invalida |

---

## COMPLETADAS

| # | Tarea | Modulo | Fecha |
|---|---|---|---|
| C-01 | Estructura de proyectos y solucion | Core | 2026-02-05 |
| C-02 | Swagger + XML docs | Core | 2026-02-05 |
| C-03 | TenantContext + TenantResolutionMiddleware | Core | 2026-02-07 |
| C-04 | CorrelationIdMiddleware | Core | 2026-02-07 |
| C-05 | GlobalExceptionMiddleware | Core | 2026-02-07 |
| C-06 | BaseApiController | Core | 2026-02-07 |
| C-07 | ErrorResponse estandar | Core | 2026-02-07 |
| C-08 | Product aggregate (dominio) | Catalog | 2026-02-14 |
| C-09 | ProductStatus enum y transiciones | Catalog | 2026-02-14 |
| C-10 | IProductRepository + IProductService | Catalog | 2026-02-14 |
| C-11 | ProductService (monolitico, pendiente refactor) | Catalog | 2026-02-14 |
| C-12 | Admin + Public ProductsController | Catalog | 2026-02-14 |
| C-13 | FluentValidation para Products | Catalog | 2026-02-14 |
| C-14 | AppEntity base con auditoria | Core | 2026-02-07 |
| C-15 | EfProductRepository + migraciones iniciales | Catalog | 2026-02-07 |
| C-16 | Result<T> en dominio | Core | 2026-02-07 |
| C-17 | JWT Bearer (Keycloak) configurado | Auth | 2026-02-14 |
| C-18 | Authorization policies iniciales | Auth | 2026-02-14 |
| C-19 | Coleccion Postman MVP | Docs | 2026-02-07 |
| C-20 | documentation.md consolidado v2.0 | Docs | 2026-02-17 |
| C-21 | Fase 2 seguridad completada (OIDC, RBAC, CORS, headers, UserContext, tests) | Auth | 2026-02-21 |
| C-22 | F2-01 [Authorize] en endpoints admin | Auth | 2026-02-21 |
| C-23 | F2-02 CORS por ambiente | Auth | 2026-02-21 |
| C-24 | F2-03 UserContext completo | Auth | 2026-02-21 |
