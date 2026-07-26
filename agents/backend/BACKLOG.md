# BACKLOG - Kanban eShopy Backend

> Estado al 2026-07-26. Reauditado contra el codigo real en HEAD (d531917, ultimo commit 2026-02-20) tras una pausa de ~5 meses.
> B-01, B-03 y P-01 estaban marcados como pendientes pero el codigo ya los resuelve desde el commit `35cebe9` (refactor CQRS) — se movieron a COMPLETADAS. Se agrego una seccion nueva de deuda tecnica de arquitectura (D-xx) que no estaba trackeada; D-02 y D-04 se implementaron y verificaron el mismo dia. D-01 (Unit of Work explicito) se probo y se revirtio a proposito — ver nota debajo de la tabla.
> Mismo dia: Fase 4 completa (Tenants + Store + Subscription minima) con infra Docker Compose para SQL Server + Keycloak. Ver C-31 en adelante. Fase 6 (Carrito, C-43), Fase 7 (Pedidos + minimo de Pagos, C-44..C-46) y el webhook de Fase 8 (C-47..C-49) tambien completados el mismo dia; C-45 documenta un bug real de concurrencia encontrado y corregido en vivo. Fase 8 solo le falta a los adapters reales de Bancard/PagoPar (F8-03/04), bloqueados sin su documentacion de API. B-02, F6-04 y F9-01/F9-03 (C-50..C-52) tambien cerrados el mismo dia — C-52 documenta dos bugs reales mas encontrados en el smoke test (paralelizacion de tests con WebApplicationFactory, orden de middleware para enrichment de logs).

---

## BLOQUEANTE (debe resolverse antes de continuar)

_(vacio — B-02 resuelto, ver COMPLETADAS C-50)_

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

### Fase 4 - Tenants (Onboarding) — completa, ver COMPLETADAS C-31..C-37, C-41
| # | Tarea | Descripcion |
|---|---|---|
| F4-06 | Precios reales de planes | `PlanPricing.cs` retorna 0 para los 3 planes (GOVERNANCE.md los marca TBD). Reemplazar cuando el negocio defina precios |
| F4-07 | Secret management para Keycloak Admin API en produccion | `appsettings.Production.json` sigue con un placeholder de secret; inyectar via secret store real antes de deployar |

### Fase 5 - Catalog (refactor)
| # | Tarea | Descripcion |
|---|---|---|
| F5-01 | Commands separados de Queries | Separar ProductService en Command/Query handlers |
| F5-02 | CurrencyCode desde Store | Eliminar "PYG" hardcodeado |
| F5-03 | Auditoria de cambios (precio/estado) | La tabla `AuditLogs` y `IAuditLogger` ya existen (F9-03, C-52) — falta solo instrumentar `ChangeProductStatusCommandHandler`/`UpdateProductCommandHandler` con una llamada a `LogAsync`, no requiere trabajo de infraestructura nuevo |
| F5-04 | ProductImages (metadata imagenes) | Entidad + endpoint de imagenes |

### Fase 6 - Carrito — completa (incl. F6-04), ver COMPLETADAS

### Fase 7 - Pedidos — completa, ver COMPLETADAS C-44..C-46 y `domain/orders.md`

### Fase 8 - Pagos — F8-01/02/05/06 completos (ver C-47..C-49), solo faltan los adapters reales
| # | Tarea | Descripcion |
|---|---|---|
| F8-03 | BancardAdapter | Integracion con Bancard API — bloqueado hasta tener la documentacion real del provider |
| F8-04 | PagoParAdapter | Integracion con PagoPar API — idem |

### Fase 9 - Observabilidad — F9-01/F9-03 completos (ver C-51..C-52), F9-02 pendiente
| # | Tarea | Descripcion |
|---|---|---|
| F9-02 | OpenTelemetry traces y metricas | Instrumentacion basica — mas util cuando haya mas de un servicio corriendo; hoy es un unico backend monolitico. Explicitamente no encarado el 2026-07-26 (decision del usuario) |

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
| C-41 | F4-05 Invitar Admin/Staff (`GET/POST /api/admin/users`) — `IKeycloakUserProvisioner` generalizado a cualquier `TenantUserRole` (no solo Owner); verificado en vivo contra Keycloak/SQL Server reales | Tenants | 2026-07-26 |
| C-42 | `PagedResult<T>.TotalPages` (computado) — `api-contracts.md` ya lo documentaba en toda respuesta paginada, el DTO no lo tenia | Core | 2026-07-26 |
| C-43 | F6-01/02/03 `Cart` + `CartItem` completos — primer agregado del proyecto con coleccion hija encapsulada (`Items` respaldado por campo privado, `PropertyAccessMode.Field`). `GET/POST/PUT/DELETE /api/cart[/items/{productId}]`, `IProductRepository.GetByIdsAsync` (batch, evita N+1 en el DTO). Verificado en vivo: acumular, listar, actualizar, eliminar, contra SQL Server real | Carts | 2026-07-26 |
| C-44 | F7-01..F7-05 Fase 7 (Pedidos) completa: `Order`/`OrderItem` (coleccion encapsulada, mismo patron que Cart), `ICheckoutWriter` (writer angosto Order+Payment+TenantCounter, sin SQL crudo), `TenantCounter` con `CurrentValue` como concurrency token EF para `OrderNumber` atomico. Incluye F8-01/F8-02 minimos como prerequisito: `Payment` entidad + `IPaymentProviderAdapter.InitiateAsync` + `FakePaymentProviderAdapter`. `POST /api/checkout` (anonimo, header `X-Cart-Token`) + `GET /api/orders[/{id}]` + `PATCH /api/orders/{id}/status` (admin, `OrdersRead`/`OrdersWrite`). FK circular Order↔Payment resuelta dando la FK real solo a `Payments.OrderId` | Orders/Payments | 2026-07-26 |
| C-45 | Bug real (encontrado en smoke test de concurrencia contra SQL Server real, 25 checkouts simultaneos): el retry loop de `EfCheckoutWriter` solo atrapaba `DbUpdateConcurrencyException`, pero bajo contencion real el perdedor de la carrera a veces recibe una violacion de indice unico cruda (`SqlException` 2601 sobre `UQ_Orders_TenantId_OrderNumber`) en su lugar — el `UPDATE` del counter puede afectar 0 filas sin abortar el resto del batch, dejando que el `INSERT` de `Order` choque contra un `OrderNumber` ya tomado. Fix: atrapar tambien `DbUpdateException` cuando envuelve `SqlException` 2601/2627 y reintentar igual. Bug secundario relacionado: `Order.AssignOrderNumber` tiraba si se llamaba dos veces, lo que rompia cualquier reintento sobre la misma instancia — se hizo idempotente a proposito. Verificado: 0 duplicados, 0 gaps, contador consistente tras el fix | Orders | 2026-07-26 |
| C-46 | Tests Fase 7: `OrderTests`/`PaymentTests` (dominio, incluye todas las transiciones validas/invalidas), `CheckoutCommandValidatorTests`, `CheckoutFlowTests` (integracion end-to-end con fakes: checkout completo, email invalido, transicion de estado invalida) — 115 tests unitarios, 17 de integracion, todos verdes | Orders/Payments | 2026-07-26 |
| C-47 | F8-01/02/05/06 Webhook de pagos completo: `PaymentEventProcessed` (idempotencia, tabla global sin TenantId), `IPaymentWebhookWriter` (writer angosto, mismo patron que `ICheckoutWriter`), `ProcessPaymentWebhookCommandHandler` (resuelve tenant sin subdominio via `TenantContext.Set(tenantId)`, ahora con `subdomain` opcional). `POST /api/payments/webhooks/{provider}` publico, excluido de `TenantResolutionMiddleware`. `Payment.ChangeStatus` gana la transicion `Initiated → Captured` (varios gateways de redirect confirman en un unico webhook, sin paso de autorizacion separado) | Payments | 2026-07-26 |
| C-48 | Correccion de diseño durante la implementacion: `IPaymentProviderAdapter.ValidateWebhookSignature`/`ParseWebhook` NO toman `HttpRequest` (el diseño original si) — `EShopy.Application` no depende de ASP.NET Core, igual que el resto del proyecto. `PaymentsController` lee el body/headers crudos y se los pasa como `(string rawBody, IReadOnlyDictionary<string,string> headers)`. `FakePaymentProviderAdapter` implementa un formato de firma/payload propio (header `X-Fake-Signature` + JSON `{eventId, providerPaymentId, eventType}`), documentado como NO el formato de ningun provider real — permite ejercitar el codigo real del webhook (firma, idempotencia, transiciones) en dev/tests sin esperar la documentacion de Bancard/PagoPar | Payments | 2026-07-26 |
| C-49 | Tests Fase 8: `PaymentTests` actualizado (nueva transicion), `PaymentWebhookFlowTests` (integracion: captura exitosa, fallo, reenvio de EventId duplicado sin reaplicar, firma invalida → 401, `ProviderPaymentId` desconocido → 404) — 115 tests unitarios, 22 de integracion, todos verdes. Verificado en vivo contra SQL Server real: los mismos 5 casos, incluida la idempotencia (una sola fila en `PaymentEventsProcessed` tras dos webhooks con el mismo EventId) | Payments | 2026-07-26 |
| C-50 | B-02 Bootstrap de DB: `Program.cs` chequea migraciones pendientes al arrancar en Development (`db.Database.GetPendingMigrations()`, sincrono) y tira `InvalidOperationException` con mensaje claro si faltan — antes un schema desincronizado fallaba con un error de SQL confuso en el primer request que tocaba la tabla/columna faltante. Manual a proposito en Production | Core | 2026-07-26 |
| C-51 | F6-04 Limpieza de carritos expirados: `CartCleanupBackgroundService` (`IHostedService`), corre cada `CartCleanup:IntervalMinutes` (60 en Production, 1 en Development), `ICartRepository.DeleteExpiredAsync` usa `ExecuteDeleteAsync` (DELETE en bloque, cascada a `CartItems` via constraint DB, sin cargar entidades a memoria). Verificado en vivo: carrito forzado a expirado, eliminado en el siguiente ciclo | Carts | 2026-07-26 |
| C-52 | F9-01/F9-03 Serilog + AuditLog: migracion completa de `ILogger` built-in a Serilog (`Serilog.AspNetCore`), sinks Console + File (JSON compacto en Development, rolling diario). `RequestLoggingScopeMiddleware` migrado de `ILogger.BeginScope` a `Serilog.Context.LogContext.PushProperty`. `AuditLog`/`IAuditLogger`/`EfAuditLogger` (F9-03): registro append-only best-effort (nunca revierte la operacion que audita), instrumentado en 4 operaciones sensibles (`Tenant.Activate`, `Order.ChangeStatus`, `TenantUser.Invite`, `Payment.Webhook`). Dos bugs reales encontrados y corregidos en el smoke test: (1) `WebApplicationFactory` + `HostFactoryResolver` no soporta invocaciones concurrentes — `EShopy.Tests.Integration` corria clases de test en paralelo, cada una con su propia factory, y fallaban de forma intermitente ("entry point exited without building an IHost"); fix: `[assembly: CollectionBehavior(DisableTestParallelization = true)]`. (2) `RequestLoggingScopeMiddleware` corria ANTES de `UseAuthentication()`, asi que `UserId`/`Email` quedaban vacios en los logs — el `ClaimsPrincipal` todavia no estaba poblado; fix: moverlo despues de `UseAuthorization()`, y enriquecer el resumen de `UseSerilogRequestLogging()` via su propio `EnrichDiagnosticContext` (ese middleware si debe ir primero, para loguear tambien las respuestas 401/403 que nunca llegan al resto del pipeline) | Core | 2026-07-26 |
