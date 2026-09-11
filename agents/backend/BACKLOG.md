# BACKLOG - Kanban eShopy Backend

> Estado al 2026-09-09 (F9-04 reporte de errores del frontend completado, ver C-61). Antes de eso: D-03 concurrencia optimista end-to-end en Products (C-60), F5-03 (C-59), F5-02 (C-58), aislamiento de membresia por tenant (C-57), D-05 (C-56), F8-07 (C-53) y la rea auditoria del 2026-07-26.
> B-01, B-03 y P-01 estaban marcados como pendientes pero el codigo ya los resuelve desde el commit `35cebe9` (refactor CQRS) — se movieron a COMPLETADAS. Se agrego una seccion nueva de deuda tecnica de arquitectura (D-xx) que no estaba trackeada; D-02 y D-04 se implementaron y verificaron el mismo dia. D-01 (Unit of Work explicito) se probo y se revirtio a proposito — ver nota debajo de la tabla. F5-01 tenia el mismo problema que B-01/B-03/P-01 (ya resuelto por el mismo commit) — se saco de PROXIMAS, ver C-26.
> Mismo dia: Fase 4 completa (Tenants + Store + Subscription minima) con infra Docker Compose para SQL Server + Keycloak. Ver C-31 en adelante. Fase 6 (Carrito, C-43), Fase 7 (Pedidos + minimo de Pagos, C-44..C-46) y el webhook de Fase 8 (C-47..C-49) tambien completados el mismo dia; C-45 documenta un bug real de concurrencia encontrado y corregido en vivo. Fase 8 solo le falta a los adapters reales Bancard/PagoPar (F8-03/04), bloqueados sin su documentacion de API. B-02, F6-04 y F9-01/F9-03 (C-50..C-52) tambien cerrados el mismo dia — C-52 documenta dos bugs reales mas encontrados en el smoke test (paralelizacion de tests con WebApplicationFactory, orden de middleware para enrichment de logs).

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
5. **F5-04** (foto de producto) — el más grande de los cinco, habilita **GAP-02** del frontend.
6. **D-07** (Products a lote) — nueva, agregada 2026-09-08. No es urgente para nada de lo anterior, pero se resuelve antes de tocar tooling/CI (ver la nota de la tabla de deuda técnica, más abajo) y antes de que el frontend arranque cualquier pantalla de carga masiva.

Un solo prompt por ítem, como se viene haciendo — no mandar dos a la vez.

---

## BLOQUEANTE (debe resolverse antes de continuar)

_(vacio — B-02 resuelto, ver COMPLETADAS C-50)_

---

## DEUDA TECNICA / ARQUITECTURA — fase de estabilización antes de CI/CD

Esta tabla es la fase de estabilización: no bloquea el trabajo de hoy, pero
se paga caro si se ignora antes de invertir en CI/CD para este repo (ver
sección "ENTORNOS" más abajo) — automatizar tests/deploy contra
un contrato que ya sabemos que va a cambiar (D-07) es repetir el trabajo
dos veces. No bloquea el tooling del frontend (L-05/L-07 en su `BACKLOG.md`),
que es independiente de esto.

| # | Tarea | Modulo | Detalle |
|---|---|---|---|
| D-06 | Sin forma de entregar el primer acceso a un tenant nuevo | Tenants/Identity | `KeycloakAdminClient.CreateUserAsync` crea el usuario con password aleatoria y `temporary: true` (fuerza a cambiarla en el primer login), pero esa password no se devuelve en la respuesta del onboarding ni se envia por ningun lado — hoy no hay forma de que la owner reciba su primer acceso. El realm tiene `resetPasswordAllowed: true` pero **sin SMTP configurado**, asi que el link de "olvide mi contraseña" de Keycloak tampoco funciona. Encontrado el 2026-09-08 evaluando el camino a un deploy de prueba (ver `eshopy-frontend/agents/DEPLOY-READINESS.md`, item DEPLOY-02). Fix rapido propuesto: devolver el link/password temporal en la respuesta de `POST /api/onboarding/tenants` para entregarlo a mano; el fix completo (SMTP + `execute-actions-email` nativo de Keycloak) es trabajo de infraestructura aparte, no antes de tener gente ajena dandose de alta sola. **Pendiente, no implementado** |
| D-07 | Endpoints de Products nacieron singulares, no en lote | Catalog | Viola la regla nueva de `GOVERNANCE.md` ("Mutaciones en lote por defecto", 2026-09-08): `POST /api/products` crea uno, `PUT /api/products/{id}` actualiza uno, no existe `DELETE`. Se escribieron antes de que la regla existiera. Migrar a `POST /api/products/batch` (o equivalente) que reciba una lista — un alta individual sigue siendo un lote de 1, sin caso especial. Motivo concreto, no estético: el negocio va a necesitar cargar catálogos grandes (una tienda no carga 300 productos de a uno), y ese día el contrato tiene que estar listo sin romper a los clientes que ya llaman la forma singular — por eso conviene resolverlo ahora, con poco código en juego, y no después con Products en producción. El frontend no tiene que cambiar nada todavía: sigue llamando con lote de 1 hasta que exista una función real de carga masiva (ver `eshopy-frontend/agents/BACKLOG.md`, "Más adelante"). **Pendiente, no implementado** — bloquea empezar cualquier carga masiva del lado del frontend |

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

_(vacio)_

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

### Fase 5 - Catalog (refactor)
| # | Tarea | Descripcion |
|---|---|---|
| F5-04 | Foto de producto | Ver decisión completa en `GOVERNANCE.md` ("Imagen de producto: storage y mejora desacoplados"). Resumen: `Product.ImageUrl` (nullable, sin tabla `ProductImages` — una sola imagen por producto). Subida vía `IProductImageStorage` (Application), implementado hoy por `LocalDiskImageStorage` (disco local, sin volumen persistente todavía — no hace falta mientras el backend no corra en contenedor). Validar server-side: JPEG/PNG/WebP, máx. 5MB, redimensionar a máx. 1200px de lado mayor al subir; sin miniatura server-side separada, el catálogo la muestra chica vía CSS. Puerto `IImageEnhancer` declarado pero sin implementar, para una futura mejora con IA (feature premium) — deliberadamente desacoplado del storage. Migrar a blob storage (Azure/S3) y resolver volumen persistente queda pendiente junto con DEPLOY-01 (hosting real de producción), no antes |

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
