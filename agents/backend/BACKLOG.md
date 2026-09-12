# BACKLOG - Kanban eShopy Backend

> Estado al 2026-09-09 (F9-04 reporte de errores del frontend completado, ver C-61). Antes de eso: D-03 concurrencia optimista end-to-end en Products (C-60), F5-03 (C-59), F5-02 (C-58), aislamiento de membresia por tenant (C-57), D-05 (C-56), F8-07 (C-53) y la rea auditoria del 2026-07-26.
> B-01, B-03 y P-01 estaban marcados como pendientes pero el codigo ya los resuelve desde el commit `35cebe9` (refactor CQRS) — se movieron a COMPLETADAS. Se agrego una seccion nueva de deuda tecnica de arquitectura (D-xx) que no estaba trackeada; D-02 y D-04 se implementaron y verificaron el mismo dia. D-01 (Unit of Work explicito) se probo y se revirtio a proposito — ver nota debajo de la tabla. F5-01 tenia el mismo problema que B-01/B-03/P-01 (ya resuelto por el mismo commit) — se saco de PROXIMAS, ver C-26.
> Mismo dia: Fase 4 completa (Tenants + Store + Subscription minima) con infra Docker Compose para SQL Server + Keycloak. Ver C-31 en adelante. Fase 6 (Carrito, C-43), Fase 7 (Pedidos + minimo de Pagos, C-44..C-46) y el webhook de Fase 8 (C-47..C-49) tambien completados el mismo dia; C-45 documenta un bug real de concurrencia encontrado y corregido en vivo. Fase 8 solo le falta a los adapters reales Bancard/PagoPar (F8-03/04), bloqueados sin su documentacion de API. B-02, F6-04 y F9-01/F9-03 (C-50..C-52) tambien cerrados el mismo dia — C-52 documenta dos bugs reales mas encontrados en el smoke test (paralelizacion de tests con WebApplicationFactory, orden de middleware para enrichment de logs).

---

## Sesión 2026-09-11: definición larga (Store, planes, entornos), para que otro agente siga

F4-09 quedó implementado en C-63. Antes de eso, nada de código se había tocado del lado
backend salvo el fix de auth (C-62) y el ajuste al realm de Keycloak. Lo demás
—`F4-10`, la matriz de planes en `GOVERNANCE.md`, `ENTORNOS`— es definición
para que un agente sin este contexto lo implemente sin inventar alcance.
**Un solo prompt por ítem.**

**Orden sugerido**: F4-09 ya está disponible para que el frontend implemente F-12. Ver
`eshopy-frontend/agents/BACKLOG.md`, sesión 2026-09-11, para el orden que cruza ambos repos.

**Antes de tocar cualquier cosa que necesite Docker/Keycloak**: leer
`.claude/skills/run-eshopy/SKILL.md` sección "0" — el estado local (tenants,
seeds, usuarios) no se sincroniza entre máquinas, verificar en vivo, no
asumir. Y antes de dar por terminado un módulo: probarlo contra Docker real,
no solo contra los tests con fakes — ver la memoria de este proyecto
("live-test before done"), encontró 5 bugs reales que ningún test detectaba.

**No tocar sin una sesión de definición nueva**: `F4-10` (panel de tenants
SUPERADMIN, la idea está anotada pero sin alcance) y el mecanismo de gating
de planes (la lista de features por plan ya está cerrada arriba, el *cómo*
bloquearlo en código no).

---

## Sesión 2026-09-08 (segunda mitad): solo definición, nada implementado

Después de cerrar C-57 (aislamiento por tenant), el resto de esta sesión fue
puro trabajo de definición con el usuario — decidir el alcance exacto de
cada ítem pendiente **antes** de mandarle un prompt al agente de código, para
que no tenga que inventar ninguna decisión de arquitectura por su cuenta. No
se tocó una sola línea de código de producción; todo lo de abajo son
decisiones ya cerradas, documentadas en detalle en `GOVERNANCE.md` y en la
fila correspondiente de cada tarea, listas para implementar tal cual están
escritas.

**Orden sugerido para la próxima sesión** (de menor a mayor, y respetando
que algunas tareas del frontend dependen de que su contraparte del backend
ya esté cerrada — ver `eshopy-frontend/agents/BACKLOG.md`):

1. ~~**F5-02** (moneda configurable, no `"PYG"` hardcodeado)~~ — ✅ completado, ver C-58.
2. ~~**F5-03** (auditoría de precio/estado)~~ — ✅ completado, ver C-59.
3. ~~**D-03** (RowVersion cableado de verdad)~~ — ✅ completado, ver C-60; habilita **GAP-04** del frontend.
4. ~~**F9-04** (`POST /api/client-errors`)~~ — ✅ completado, ver C-61; habilita **D-02** del frontend.
5. ~~**F5-04** (foto de producto)~~ — ✅ completado, ver C-64; habilita **GAP-02** del frontend.
6. **D-07** (Products a lote) — nueva, agregada 2026-09-08. No es urgente para nada de lo anterior, pero se resuelve antes de tocar tooling/CI (ver la nota de la tabla de deuda técnica, más abajo) y antes de que el frontend arranque cualquier pantalla de carga masiva.

Un solo prompt por ítem, como se viene haciendo — no mandar dos a la vez.

---

## BLOQUEANTE (debe resolverse antes de continuar)

_(vacio — B-02 resuelto, ver COMPLETADAS C-50)_

---

## DEUDA TECNICA / ARQUITECTURA — fase de estabilización antes de CI/CD

Esta tabla es la fase de estabilización: no bloquea el trabajo de hoy, pero
se paga caro si se ignora antes de invertir en CI/CD para este repo (ver
sección "ENTORNOS" más abajo). No bloquea el tooling del frontend
(L-05/L-07 en su `BACKLOG.md`), que es independiente de esto.

_(tabla vacía por ahora — D-06 y D-07, los dos últimos items, se resolvieron el 2026-09-12)_

**D-07 (Products a lote) — ✅ resuelto 2026-09-12.** Ver la fila de "Mutaciones en lote por
defecto" en `GOVERNANCE.md` (precisada el mismo día) para la decisión completa. Resumen: `POST
/api/products` y `PUT /api/products/{id}` migraron a `POST /api/products` y `PUT /api/products`
(sin id en la ruta) — ambos reciben SIEMPRE un array (un alta o edición individual es un lote de
1, sin forma singular en paralelo), y persisten el lote completo en una sola transacción
(`IProductRepository.AddRangeAsync`/`UpdateRangeAsync`, un solo `SaveChangesAsync`): si un item
falla — validación, slug/SKU duplicado (contra la base o dentro del propio lote), RowVersion
desactualizado — no se persiste ninguno. `PATCH /api/products/{id}/status` seguió singular a
propósito (no nombrado por D-07); `DELETE` sigue sin existir, ninguna forma (no se inventó ahora).
`CreateProductCommandHandler`/`UpdateProductCommandHandler` (singulares) se eliminaron, reemplazados
por `CreateProductsCommandHandler`/`UpdateProductsCommandHandler`; los validadores por item
(`CreateProductCommandValidator`/`UpdateProductCommandValidator`) se reutilizaron sin cambios.
Verificado en vivo contra SQL Server real: batch de 2 altas, slug duplicado dentro del mismo lote
rechazado sin crear nada, batch de 2 ediciones con un solo RowVersion desactualizado rechazado sin
aplicar ninguna (ni siquiera la que tenía su RowVersion al día). 145 tests unitarios, 49 de
integración (3 nuevos), todos verdes.

**D-06 (primer acceso de un tenant nuevo) — ✅ resuelto 2026-09-12.** Fix rápido tal cual se
propuso: `IKeycloakUserProvisioner.CreateUserAsync` ahora devuelve `KeycloakUserProvisioningResult`
(`UserId` + `TemporaryPassword`, antes solo `string UserId`) — la password ya se generaba en
`KeycloakAdminClient`, solo se descartaba después de mandarla a Keycloak. `POST /api/onboarding/tenants`
la expone una sola vez en `TenantOnboardingResultDto.OwnerTemporaryPassword`, mismo criterio que
`Order.AccessToken`/`CheckoutResultDto` (nunca se persiste en texto plano, nunca se vuelve a exponer).
`InviteTenantUserCommandHandler` (el otro caller de la interfaz, `POST /api/admin/users`) solo recibió
el ajuste de compilación — sigue sin entregarle su password a un Admin/Staff invitado, mismo gap, pero
fuera de alcance de este ítem (no se pidió, no se tocó). Verificado en vivo: `POST /api/onboarding/tenants`
contra la API y Keycloak reales devuelve la password, y un intento de login por password-grant con ese
valor exacto contra Keycloak responde `"Account is not fully set up"` (no `"Invalid user credentials"`) —
confirma que es la password real, pendiente del cambio obligatorio de primer login. 145 tests unitarios,
46 de integración, todos verdes.

**D-01 (Unit of Work explicito) — descartado a proposito (2026-07-26).** Se implemento (`IUnitOfWork`/`EfUnitOfWork`) y se revirtio en la misma sesion: `EShopyDbContext` ya ES un Unit of Work (trackea cambios, `SaveChangesAsync` los confirma atomicamente); envolverlo en otra interfaz es abstraer una abstraccion sin necesidad real todavia, porque solo existe un repositorio (`IProductRepository`). El repositorio vuelve a llamar `SaveChangesAsync` directamente. **Revisar esta decision cuando exista una operacion que necesite escribir a traves de mas de un repositorio en una sola transaccion** (candidato: Checkout en F7-02 — stock + order + payment).

---

## ENTORNOS — definido 2026-09-11, nada implementado

Hoy solo existen dos entornos de hecho, ninguno formalizado más allá de un archivo de config:
`Development` (Docker local, `appsettings.Development.json`) y una intención de `Production`
(`appsettings.Production.json`, con placeholders — ver F4-07). No hay un QA intermedio, y "entorno"
hoy es sinónimo de "la base de datos de quien sea que esté corriendo `docker compose up`" — ver la
sesión del 2026-09-11 en el historial de chat: la DB local de un desarrollador acumula datos desde
que se creó el volumen Docker (en este caso, desde 2026-07-26), sin distinción entre "estoy probando
cualquier cosa" y "esta es mi tienda de verdad para mostrar". Reset completo documentado en
`docs/keycloak-setup.md` y en un `.txt` de credenciales de dev en el escritorio de cada máquina
(fuera del repo a propósito, no versionado — son passwords, aunque sean solo de dev local).

**El proyecto se trabaja desde más de una máquina física** (al menos una notebook personal y una del
trabajo, confirmado 2026-09-11). Git sincroniza código/migraciones/docs; los volúmenes Docker
(SQL Server, Keycloak) son locales a cada máquina y nunca se sincronizan — un tenant o seed creado en
una no existe en la otra, sin excepción. Documentado como regla operativa en
`.claude/skills/run-eshopy/SKILL.md` sección "0" (léela primero: verificar el estado local en vivo
antes de asumir que un tenant/usuario/seed de una sesión anterior sigue existiendo). No es un ítem
para "resolver" — es una restricción a respetar; ENV-01 (QA compartido, no en un laptop) es lo más
cerca que hay de una solución real si algún día molesta de verdad.

| # | Tarea | Detalle |
|---|---|---|
| ENV-01 | Perfil de entorno `QA` explícito | `appsettings.QA.json` (mismo patrón que Development/Production) + `ASPNETCORE_ENVIRONMENT=QA`. Un escalón estable entre "mi laptop" y Production, no reemplaza a ninguno de los dos. Dónde corre físicamente (mismo docker-compose con un profile separado vs. un servidor real compartido) depende de DEPLOY-01 (hosting real) — definir la infraestructura de QA antes de esa decisión es especular sobre algo que todavía no existe |
| ENV-02 | Enmascarar datos en QA/staging | Política, no herramienta: el día que QA (o cualquier entorno no-dev) se siembre con datos derivados de un tenant real — no hoy, no existe producción todavía — todo PII (`ownerEmail`, nombre, dirección de `Order`) se enmascara antes de cargarse ahí. Se define ahora para no improvisarlo el día que exista un tenant real que valga la pena copiar para probar con volumen realista. Hasta entonces, cualquier entorno no-dev se siembra igual que hoy: `DevSeeder` u onboarding real con datos inventados, nunca con datos de una tienda real |
| ENV-03 | Separar tenant "playground" del tenant "demo" en dev local | No es código, es disciplina: `smoketest2` (o el que sea) queda para probar cosas destructivas/masivas: como el aislamiento es por `TenantId` (Global Query Filter), lo que se rompe ahí no puede tocar otro tenant. La tienda que se cree para mostrarle a alguien de verdad se trata aparte, sin experimentos |

**Perfiles de rol/permiso**, si "perfiles del sistema" se refería a esto y no a entornos: ya existen y
están completos — `ESHOPY_SUPERADMIN`/`TENANT_OWNER`/`TENANT_ADMIN`/`TENANT_STAFF`, ver
`GOVERNANCE.md` "Roles y permisos". Nada pendiente ahí; si la idea era otra cosa (perfiles de
configuración por entorno, ya cubierto arriba en ENV-01, o algo distinto), aclarar antes de convertir
esto en una tarea.

---

## EN PROGRESO

_(vacío — F-13A aprobado por el usuario y movido a COMPLETADAS como C-67)_

---

## PROXIMAS (ordenadas por prioridad)

### Fase 3 - Persistencia base
| # | Tarea | Descripcion |
|---|---|---|
| F3-02 | Interceptor TenantId + fechas UTC | Impedir SaveChanges si TenantId ausente; setear timestamps UTC |

### Fase 4 - Tenants (Onboarding) — completa, ver COMPLETADAS C-31..C-37, C-41
| # | Tarea | Descripcion |
|---|---|---|
| F4-06 | Precios reales de planes | `PlanPricing.cs` retorna 0 para los 3 planes (GOVERNANCE.md los marca TBD). Reemplazar cuando el negocio defina precios |
| F4-07 | Secret management para Keycloak Admin API en produccion | `appsettings.Production.json` sigue con un placeholder de secret; inyectar via secret store real antes de deployar |
| F4-08 | Cambiar el subdominio de un tenant ya creado | No existe hoy — `TenantsController` solo tiene `GET` y `activate`. No es un problema de acoplamiento (confirmado 2026-09-11 grepeando cada uso de `Subdomain`: es solo una clave de busqueda con indice unico + cache ~60s en `EfTenantResolver`, nada mas lo referencia — el aislamiento real es por `TenantId`, inmutable). Cuando haga falta: mismo patron que ya existe para el `Slug` de Products — `UpdateSubdomainCommand`, valida unicidad, `Tenant.ChangeSubdomain()` en dominio, invalidar el cache del resolver. Sin prioridad hasta que alguien lo pida de verdad |
| F4-10 | Panel de administracion de tenants para SUPERADMIN | Idea capturada 2026-09-11, **sin definir en detalle a proposito** — el usuario pidio anotarla, no encararla todavia ("eso luego"). Hoy crear/activar/listar tenants es manual via Postman/curl contra `OnboardingController`/`TenantsController` (asi se creo el tenant Franja en esta sesion) — no escala a administrar varios tenants de verdad. Cuando se encare: necesita su propia sesion de definicion (que puede hacer un SUPERADMIN ademas de listar/activar — ¿suspender, cambiar plan, ver metricas por tenant?), toca backend (nuevos endpoints admin, hoy `TenantsController` solo tiene `GET`/`activate`) y frontend (una seccion nueva, no confundir con el Admin de cada tenant que ya existe). No arrancar sin esa definicion |

### Fase 5 - Catalog (refactor)
| # | Tarea | Descripcion |
|---|---|---|
| F5-04 | Foto de producto — ✅ hecho (2026-09-12) | `Product.ImageUrl`, endpoint multipart y `IProductImageStorage` implementados. `LocalDiskImageStorage` verifica JPEG/PNG/WebP real y 5 MB, corrige orientación, limita a 1200 px y guarda WebP en `App_Data/uploads/products`; los DTO públicos/admin exponen la URL. Conserva concurrencia por `rowVersion`, elimina el archivo nuevo si falla la persistencia y reemplaza el anterior solo después del commit. `IImageEnhancer` queda separado y sin implementación. Aprobado por el usuario |

**Moneda de exhibición vs. moneda de liquidación — deliberadamente NO implementado (2026-09-08), solo marcado.** Idea del usuario: una tienda podría querer *mostrar* precios en una moneda (ej. USD) y *cobrar* en otra (ej. PYG vía Bancard/PagoPar, que solo liquidan en guaraníes), con la conversión resuelta antes de pagar. No se construye ahora: no hay un segundo país/gateway concreto todavía, necesitaría una fuente de tipo de cambio y reglas de redondeo propias — sería la misma especulación sin caso de uso real que se evitó con F5-04. Lo que sí queda, para no tener que rediseñar nada el día que haga falta: un comentario ancla en `Store.CurrencyCode` (`EShopy.Domain/Tenants/Store.cs`) señalando que hoy esa única propiedad cumple los dos roles (moneda de exhibición y de liquidación) y que es el punto exacto a separar si algún día se necesita distinguirlos — no una interfaz ni un servicio vacío, solo la señal de dónde enganchar.

### Fase 6 - Carrito — completa (incl. F6-04), ver COMPLETADAS

### Fase 7 - Pedidos — completa, ver COMPLETADAS C-44..C-46 y `domain/orders.md`

### Fase 8 - Pagos — F8-01/02/05/06/07 completos (ver C-47..C-49, C-53), solo faltan los adapters reales
| # | Tarea | Descripcion |
|---|---|---|
| F8-03 | BancardAdapter | Integracion con Bancard API — bloqueado hasta tener la documentacion real del provider |
| F8-04 | PagoParAdapter | Integracion con PagoPar API — idem |

### Fase 9 - Observabilidad — F9-01/F9-03/F9-04 completos (ver C-51..C-52, C-61), F9-02 pendiente
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
| C-53 | F8-07 Consulta publica del pedido: `Order.AccessToken` (Base64Url de 32 bytes, generado en `Order.Create`) devuelto una sola vez en `CheckoutResultDto`; `GET /api/public/orders/{id}` (anonimo, header `X-Order-Token`) devuelve `PublicOrderDto` — vista reducida sin email/nombre/direccion del comprador ni el propio token. Autoriza por posesion del secreto, no por sesion (el comprador es anonimo en el MVP). Comparacion en tiempo constante (`CryptographicOperations.FixedTimeEquals`); token que no coincide responde 404 y no 403, para no permitir enumerar pedidos del tenant. Migracion `AddOrderAccessToken` (default `''`: los pedidos previos quedan fuera de esta via). Descartado a proposito reusar el `CartToken` — el storefront lo rota y descarta tras el checkout. 7 tests unitarios + 6 de integracion nuevos (122 unit / 28 integracion, todos verdes) y verificado en vivo contra SQL Server real, incluido el flujo completo checkout → webhook `Captured` → la consulta publica pasa a `Paid`. Sin bugs nuevos. Detecto D-05 (fechas sin `Z` en el JSON) | Orders | 2026-09-06 |
| C-54 | **Bug real (encontrado en vivo desde el frontend, 2026-09-06)**: agregar un producto DISTINTO a un carrito ya persistido devolvia 409 `CONCURRENCY_CONFLICT`. Causa: EF trata las PK `Guid` como store-generated por convencion, y usa el valor de la PK para decidir el estado de una entidad descubierta dentro de un agregado ya trackeado (PK en default = Added, PK con valor = Modified). Como todas las factories del dominio hacen `Guid.NewGuid()`, EF concluia que el `CartItem` nuevo ya existia y emitia `UPDATE ... WHERE Id = @p` en vez de `INSERT` — 0 filas afectadas → `DbUpdateConcurrencyException` → 409. No se veia al crear el carrito (`Add` marca todo el grafo `Added`), ni al acumular el mismo producto (muta un item ya trackeado), ni en los tests de integracion (usan `InMemoryCartRepository`). Fix: convencion global en `OnModelCreating` que marca toda PK `Guid` como `ValueGenerated.Never` — el dominio las asigna, no la base. `Order`/`OrderItem` tenia la misma bomba sin estallar (hoy los pedidos se crean completos en el checkout y nunca reciben un item despues), por eso la convencion es global y no una linea en `CartItemConfiguration`. `CartFlowTests` tenia el mismo hueco que el smoke test manual: acumulaba el mismo producto pero nunca agregaba uno distinto — caso agregado, aunque con el repositorio in-memory pasa igual: lo que atrapa este bug es correr contra SQL Server real | Core/Carts | 2026-09-06 |
| C-55 | `DevSeeder` (`EShopy.Api/DevTools/`, modo `dotnet run -- seed [subdominio]`): siembra un tenant + tienda + 3 productos activos directo por EF, sin pasar por Keycloak. Encontrado al levantar el entorno desde cero para un smoke test del frontend: el unico camino documentado para crear un tenant (`POST /api/onboarding/tenants`) crea el owner en Keycloak primero, asi que sin el realm arriba no hay forma de probar el storefront. Solo corre en Development (mismo guard que B-02) y es idempotente (no hace nada si el subdominio ya existe). De paso se confirmo y documento que la API arranca y sirve el catalogo publico sin Keycloak: `AddJwtBearer` resuelve su metadata OIDC recien al validar un token, no al iniciar (nota agregada a `backend-overview.md`). Tambien se documento en el README que `main` se promueve desde `develop` recien al cerrar una fase — se detecto porque un checkout probado contra `main` no traia el `AccessToken` de C-53/F8-07, que si estaba en `develop` desde el 2026-09-06 | Core/Docs | 2026-09-07 |
| C-56 | D-05 resuelto: `UtcDateTimeConverter`/`UtcNullableDateTimeConverter` (`EShopy.Api/Common/Json/`), registrados globalmente en `AddControllers().AddJsonOptions(...)`. Normalizan cualquier `DateTime`/`DateTime?` a `DateTimeKind.Utc` antes de serializar, sin importar el Kind con el que EF la haya devuelto — no hizo falta tocar los DTOs uno por uno. Verificado en vivo contra SQL Server real: `GET /api/public/orders/{id}` ahora devuelve `"createdAtUtc":"...Z"` en vez de sin sufijo. 122 unit + 28 integracion siguen en verde, ningun test asumia el formato exacto sin `Z` | Core | 2026-09-07 |
| C-57 | Aislamiento de usuarios autenticados por tenant: `TenantMembershipMiddleware` valida una membresia activa en `TenantUsers` por `(TenantId resuelto desde Host, sub del JWT)` despues de auth; `ESHOPY_SUPERADMIN`, requests anonimas y rutas sin tenant quedan exceptuados por diseño. Antes, las policies solo comprobaban permisos globales y un usuario podia reutilizar `catalog.write`/`orders.write` contra cualquier tienda cambiando el Host. `ITenantUserRepository` incorpora el lookup activo y se agregan 8 casos de integracion multi-tenant (Owner/Admin/Staff, SUPERADMIN, inactivo y rutas anonimas) | Auth/Tenants | 2026-09-08 |
| C-58 | F5-02 Moneda configurable por tenant: `CreateTenantCommand.CurrencyCode` requerido (3 letras, validado por forma) y normalizado a mayúsculas en `Store.CreateDefault`; se elimina el hardcode de `PYG` del onboarding. `PlanPricing` queda separado para la moneda de la suscripción. Tests de validación, dominio e integración y contrato/Postman actualizados | Tenants/Store | 2026-09-08 |
| C-59 | F5-03 Auditoria de precio/estado de Products: `UpdateProductCommandHandler` registra `Product.ChangePrice` solo cuando cambia el precio y `ChangeProductStatusCommandHandler` registra `Product.ChangeStatus` tras una transición válida. Ambos usan `IAuditLogger` best-effort después de persistir; detalles old→new. Dos tests de integración cubren precio+estado y ausencia de ruido cuando el precio no cambia | Catalog/Observabilidad | 2026-09-08 |
| C-60 | D-03 Concurrencia optimista end-to-end en Products: `ProductAdminDto.RowVersion` base64, PUT/PATCH lo exigen y validan, handlers rechazan token obsoleto con 409 y `EfProductRepository.UpdateAsync` fija el token del cliente como `OriginalValue` para cubrir carreras hasta `SaveChangesAsync`. Fake de integración simula rowversion; tests cubren token faltante, inválido y obsoleto en update/status. Sin migración nueva | Catalog | 2026-09-08 |
| C-61 | F9-04 Reporte de errores del frontend: `POST /api/client-errors` anónimo, excluido de `TenantResolutionMiddleware`, logging estructurado vía Serilog/`ILogger` sin tabla, truncado de message/stack a 2000 caracteres, URL a 2048 y user-agent a 512, con límite de request de 16 KB. Best-effort: responde 204 aun si falla el sink. Tests cubren acceso anónimo sin tenant, truncado y fallo del logger | Core/Observabilidad | 2026-09-09 |
| C-62 | **Bug real (encontrado en vivo, primer login de Admin de un tenant creado por el onboarding real, no por un usuario pre-cargado en `realm-eshopy.json`)**: un Owner recién creado no tenía ningún permiso — `store.write`, `catalog.write`, etc. — pese a que `KeycloakAdminClient` sí le asignaba el rol `TENANT_OWNER`. Causa: `TENANT_OWNER`/`TENANT_ADMIN`/`TENANT_STAFF`/`ESHOPY_SUPERADMIN` nunca fueron roles compuestos en Keycloak (`composite: false`); el mapper `permissions` (`oidc-usermodel-realm-role-mapper`) solo vuelca roles *directamente* asignados al claim, así que asignar el rol de grupo no traía consigo los permisos finos. Invisible hasta ahora porque cada prueba en vivo anterior usaba `owner@tenant1.local` y compañía, que en `realm-eshopy.json` tienen los permisos finos asignados a mano uno por uno, nunca a través del rol de grupo — el camino real de onboarding nunca se había ejercitado con un login de verdad. Fix: los 4 roles de grupo pasan a `composite: true` con sus permisos finos como `composites.realm`, matcheando la matriz ya documentada en `docs/keycloak-setup.md` §3 — Keycloak resuelve las composites al emitir el token, sin tocar `KeycloakAdminClient.cs`. Bug secundario relacionado, mismo login: la solicitud OIDC pedía `scope=openid profile email` pero el realm no tenía definidos los client scopes `profile`/`email` (`invalid_scope`) — se agregaron con los mappers estándar de Keycloak y se asignaron como default a `eshopy-admin`/`eshopy-api`. Aplicado en vivo vía Admin API y persistido en `realm-eshopy.json` para que un `--import-realm` futuro no vuelva a nacer roto | Auth/Tenants | 2026-09-11 |
| C-63 | F4-09 personalización ampliada de Store: seis columnas reales de contacto (`ContactWhatsapp`, `ContactEmail`, `InstagramUrl`, `FacebookUrl`, `Address`, `BusinessHours`) y cuatro knobs cosméticos tipados en `StoreTheme` sobre `Data` (`FontFamily`, `HeadingScale`, `BorderRadius`, `SpacingDensity`). GET/PUT `/api/store`, validadores, dominio, mapping, migración y Postman actualizados. 145 tests unitarios y 46 de integración verdes; el flujo de integración comprueba que una actualización autenticada aparece después en la lectura pública | Tenants/Store | 2026-09-11 |
| C-64 | F5-04 imagen de producto: `Product.ImageUrl`, endpoint multipart protegido por `CatalogWrite` y `rowVersion`, validación real de JPEG/PNG/WebP hasta 5 MB, orientación automática, límite de 1200 px y normalización WebP. Storage local con URLs públicas inmutables y limpieza segura de archivos reemplazados o fallidos. Contratos públicos/admin, migración, documentación y Postman actualizados | Catalog | 2026-09-12 |
| C-65 | D-06 primer acceso de un tenant nuevo: `IKeycloakUserProvisioner.CreateUserAsync` devuelve `KeycloakUserProvisioningResult` (UserId + TemporaryPassword, antes solo el id); `POST /api/onboarding/tenants` expone `OwnerTemporaryPassword` una sola vez en `TenantOnboardingResultDto`, mismo criterio que `Order.AccessToken`. Verificado en vivo contra Keycloak real (password-grant con la password devuelta responde "Account is not fully set up", no "Invalid user credentials") | Tenants/Identity | 2026-09-12 |
| C-66 | D-07 Products a lote: `POST/PUT /api/products` migraron a lote (sin forma singular en paralelo), persistencia todo-o-nada vía `IProductRepository.AddRangeAsync`/`UpdateRangeAsync` (un solo `SaveChangesAsync`). Duplicados de slug/SKU detectados tanto contra la base como dentro del propio lote. `PATCH .../status` y DELETE quedaron fuera a propósito. Verificado en vivo contra SQL Server real: batch de altas, slug duplicado intra-lote rechazado sin crear nada, un RowVersion desactualizado en un batch de 2 ediciones rechaza ambas | Catalog | 2026-09-12 |
| C-67 | F-13A subida real de logo y portada de Store: endpoints multipart/DELETE protegidos por `StoreWrite`, validación real JPEG/PNG/WebP hasta 5 MB, orientación, WebP y límites de 800/1920 px. URLs públicas inmutables por Store y limpieza posterior a la persistencia, restringida a la carpeta del tenant. Sin migración: reutiliza `LogoUrl` y `StoreTheme.HeroImageUrl`. Aprobado por el usuario; suite no ejecutada por instrucción explícita | Tenants/Store | 2026-09-12 |
