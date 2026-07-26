# BACKLOG - Kanban eShopy Backend

> Estado al 2026-07-26. Reauditado contra el codigo real en HEAD (d531917, ultimo commit 2026-02-20) tras una pausa de ~5 meses.
> B-01, B-03 y P-01 estaban marcados como pendientes pero el codigo ya los resuelve desde el commit `35cebe9` (refactor CQRS) — se movieron a COMPLETADAS. Se agrego una seccion nueva de deuda tecnica de arquitectura (D-xx) que no estaba trackeada; D-02 y D-04 se implementaron y verificaron el mismo dia. D-01 (Unit of Work explicito) se probo y se revirtio a proposito — ver nota debajo de la tabla.
> Mismo dia: Fase 4 completa (Tenants + Store + Subscription minima) con infra Docker Compose para SQL Server + Keycloak. Ver C-31 en adelante.

---

## BLOQUEANTE (debe resolverse antes de continuar)

| # | Tarea | Modulo | Detalle |
|---|---|---|---|
| B-02 | Endurecer bootstrap de DB en Development | Core | Evitar drift entre schema y `__EFMigrationsHistory`; evaluar auto-migracion controlada. Confirmado: no existe ningun `Database.Migrate()`/`EnsureCreated()` en el arranque, todo es manual via `dotnet ef database update` |

---

## DEUDA TECNICA / ARQUITECTURA (no bloqueante hoy, pero se paga caro si se ignora antes de Fase 6-8)

| # | Tarea | Modulo | Detalle |
|---|---|---|---|
| D-03 | RowVersion sin uso end-to-end | Catalog | `Product.RowVersion` esta configurado como concurrency token en EF (`IsRowVersion()`) pero los comandos `Update`/`ChangeStatus` no reciben la version del cliente — se relee fresco antes de aplicar el cambio, asi que el token no previene lost updates en la practica. Decidir: cablearlo via DTO/If-Match, o sacarlo para no dar falsa sensacion de seguridad. **Pendiente, no implementado** |

**D-01 (Unit of Work explicito) — descartado a proposito (2026-07-26).** Se implemento (`IUnitOfWork`/`EfUnitOfWork`) y se revirtio en la misma sesion: `EShopyDbContext` ya ES un Unit of Work (trackea cambios, `SaveChangesAsync` los confirma atomicamente); envolverlo en otra interfaz es abstraer una abstraccion sin necesidad real todavia, porque solo existe un repositorio (`IProductRepository`). El repositorio vuelve a llamar `SaveChangesAsync` directamente. **Revisar esta decision cuando exista una operacion que necesite escribir a traves de mas de un repositorio en una sola transaccion** (candidato: Checkout en F7-02 — stock + order + payment).

---

## EN PROGRESO

_(vacio)_

---

## PROXIMAS (ordenadas por prioridad)

### Fase 3 - Persistencia base
| # | Tarea | Descripcion |
|---|---|---|
| F3-02 | Interceptor TenantId + fechas UTC | Impedir SaveChanges si TenantId ausente; setear timestamps UTC |
| F3-04 | Migracion completa con todas las tablas | Orders, Payments, Carts (Tenants/Stores/Subscriptions ya migrados, ver Fase 4) |

### Fase 4 - Tenants (Onboarding) — completa, ver COMPLETADAS C-31..C-37
| # | Tarea | Descripcion |
|---|---|---|
| F4-05 | Invitar TenantUsers adicionales | `GET/POST /api/admin/users` — solo el Owner se crea hoy (onboarding) |
| F4-06 | Precios reales de planes | `PlanPricing.cs` retorna 0 para los 3 planes (GOVERNANCE.md los marca TBD). Reemplazar cuando el negocio defina precios |
| F4-07 | Secret management para Keycloak Admin API en produccion | `appsettings.Production.json` sigue con un placeholder de secret; inyectar via secret store real antes de deployar |

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
| C-15b | F3-01 Global Query Filter por TenantId (Products) | Core | 2026-02-07 |
| C-16 | Result<T> en dominio | Core | 2026-02-07 |
| C-17 | JWT Bearer (Keycloak) configurado | Auth | 2026-02-14 |
| C-18 | Authorization policies iniciales | Auth | 2026-02-14 |
| C-19 | Coleccion Postman MVP | Docs | 2026-02-07 |
| C-20 | documentation.md consolidado v2.0 | Docs | 2026-02-17 |
| C-21 | Fase 2 seguridad completada (OIDC, RBAC, CORS, headers, UserContext, tests) | Auth | 2026-02-21 |
| C-22 | F2-01 [Authorize] en endpoints admin | Auth | 2026-02-21 |
| C-23 | F2-02 CORS por ambiente | Auth | 2026-02-21 |
| C-24 | F2-03 UserContext completo | Auth | 2026-02-21 |
| C-25 | Baseline dev estabilizado (Postman, audience JWT, connection string y migraciones) | Docs/Core | 2026-02-21 |
| C-26 | B-01/F5-01 Commands/Queries separados con `Result<T>` (ProductService eliminado) | Catalog | 2026-02-20 |
| C-27 | B-03 Paginacion SQL real (`Skip/Take` + `LongCountAsync`) en `EfProductRepository` | Catalog | 2026-02-20 |
| C-28 | P-01 `EfProductRepository` completo para el alcance actual (Add/Update/GetById/GetBySlug/paginado/unicidad) | Catalog | 2026-02-20 |
| C-29 | D-02 `GlobalExceptionMiddleware` mapea `DbUpdateConcurrencyException` y violacion de indice unico (`SqlException` 2601/2627) a 409 Conflict | Core | 2026-07-26 |
| C-30 | D-04 `SubdomainResolver` extraido a `Application/Common/Tenants` (puro, testeable) + 9 tests unitarios | Core/Tenants | 2026-07-26 |
| C-31 | Docker Compose para SQL Server + Keycloak + su Postgres, reemplaza el setup manual | Infra | 2026-07-26 |
| C-32 | F4-01 `Tenant`/`Store`/`TenantUser` (dominio) + `Subscription` (dominio, minima) | Tenants | 2026-07-26 |
| C-33 | F4-02 `CreateTenantCommand` (onboarding) con `Result<T>`, `ITenantOnboardingWriter` para atomicidad Tenant+Store+TenantUser+Subscription | Tenants | 2026-07-26 |
| C-34 | F4-03 `KeycloakAdminClient` — integracion real con la Admin API de Keycloak (crea Owner, asigna rol `TENANT_OWNER`), reutiliza el service account de `eshopy-api` | Tenants | 2026-07-26 |
| C-35 | Activacion manual SUPERADMIN (`POST /api/admin/tenants/{id}/activate`) — unico trigger de `PendingPayment → Active` hasta que exista Payments (Fase 8) | Tenants | 2026-07-26 |
| C-36 | `GET`/`PUT /api/store` real (reemplaza el skeleton hardcodeado); `EfStoreService`/`EfTenantResolver` reemplazan los placeholders in-memory, con cache ~60s en la resolucion de tenant | Tenants/Store | 2026-07-26 |
| C-37 | FKs `Products.TenantId/StoreId → Tenants/Stores` (antes Guid sueltos, marcados PENDIENTE en database-schema.md); tests de dominio y flujo de onboarding end-to-end | Catalog/Tenants | 2026-07-26 |
| C-38 | F3-03 superado: `InMemoryTenantResolver` (placeholder in-memory) reemplazado por completo por `EfTenantResolver` (ver C-36) | Tenants | 2026-07-26 |
| C-39 | Bug real (encontrado en smoke test contra DB/Keycloak reales): Global Query Filter tiraba `InvalidOperationException` ("Nullable object must have a value") en cualquier query multi-tenant hecha desde una ruta sin tenant resuelto (ej. `/api/admin/tenants/*`). EF Core evalua `.Value` de un `Guid?` de forma ansiosa al armar el parametro SQL, incluso en la rama del `\|\|` que la logica nunca deberia alcanzar. Fix: comparar `Guid?` directo (`tenantContext.TenantId == null \|\| x.TenantId == tenantContext.TenantId`) en las 4 entidades multi-tenant (`EShopyDbContext.cs`) | Core | 2026-07-26 |
| C-40 | Bug real (mismo smoke test): el service account de `eshopy-api` no tenia realmente los roles `manage-users`/`view-users`/`view-realm` de `realm-management` — la entrada manual en `realm-eshopy.json` (`users[].clientRoles`) no se aplica de forma confiable durante `--import-realm`. Se agrego el client scope `client-roles` (mapea `resource_access` al token) y se documento el paso de grant manual de una sola vez en `docs/keycloak-setup.md` | Auth/Tenants | 2026-07-26 |
