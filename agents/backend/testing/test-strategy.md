# Testing — Strategy

> Pirámide de tests, herramientas y convenciones del proyecto eShopy.

## Pirámide de tests

| Nivel | % objetivo | Qué cubre | Velocidad |
|---|---|---|---|
| Unit | 70–80% | Dominio, validadores, handlers Result<T> | Rápido |
| Integration | 20–25% | EF Core, aislamiento multi-tenant, webhooks | Medio |
| E2E | 5–10% | Flujos críticos de usuario (post-MVP, Playwright) | Lento |

## Herramientas

| Herramienta | Uso |
|---|---|
| xUnit | Framework de tests |
| FluentAssertions | Assertions legibles (`result.Should().Be(...)`) |
| NSubstitute | Mocking de dependencias |
| Testcontainers (SQL Server) o LocalDB | Base de datos real en integration tests |
| Respawn | Reset de DB entre tests de integración |
| WireMock.Net | Simular APIs de payment providers |

## Proyectos de test

```
EShopy.Tests.Unit/
  Domain/
    Products/
      ProductTests.cs          ← reglas invariantes del aggregate
      ProductStatusTests.cs    ← transiciones de estado
  Application/
    Products/
      Validators/
        CreateProductRequestValidatorTests.cs
        UpdateProductRequestValidatorTests.cs
      Handlers/
        CreateProductHandlerTests.cs
        ChangeStatusHandlerTests.cs

EShopy.Tests.Integration/
  Products/
    ProductRepositoryTests.cs  ← EF Core real
    ProductsApiTests.cs        ← endpoints via WebApplicationFactory
  MultiTenancy/
    TenantIsolationTests.cs    ← aislamiento entre tenants
  Payments/
    WebhookIdempotencyTests.cs ← idempotencia de webhooks
```

## Convenciones de nombrado

```csharp
// Nombre del método de test: Given_When_Then
[Fact]
public void Create_WithValidData_ReturnsDraftProduct()

[Fact]
public void Create_WithNegativePrice_ThrowsDomainException()

[Fact]
public void ChangeStatus_FromDraftToArchived_IsInvalid()
```

## Tests unitarios — qué testear

### Dominio (Product)
- `Create()` con datos válidos → Status = Draft, campos normalizados
- `Create()` con precio negativo → `DomainException`
- `Create()` con StockOnHand negativo → `DomainException`
- `Create()` con slug vacío → `DomainException`
- `Create()` con SKU > 64 chars → `DomainException`
- `UpdateDetails()` actualiza campos correctamente
- `ChangeStatus()` persiste nuevo status

### Validadores (FluentValidation)
- `CreateProductRequest` con slug inválido → error de validación
- `CreateProductRequest` sin nombre → error de validación
- `CreateProductRequest` con precio negativo → error de validación
- Todos los campos requeridos ausentes → lista de errores correcta

### Handlers (Application) — con NSubstitute
- `CreateProductHandler`: verifica slug único antes de crear
- `CreateProductHandler`: verifica sku único si se provee
- `ChangeStatusHandler`: verifica transición válida → `Result.Fail(PRODUCT_INVALID_STATE)`
- `ChangeStatusHandler`: producto no encontrado → `Result.Fail(NOT_FOUND)`

## Tests de integración — qué testear

### EF Core (repositorio real)
- `AddAsync` + `GetByIdAsync` — persistencia correcta
- `GetAdminListAsync` — filtra por TenantId (no ve productos de otro tenant)
- `GetPublicListAsync` — solo Status = Active
- `SlugExistsAsync` — detecta duplicado en mismo tenant, no en otro
- Constraint UNIQUE `(TenantId, Slug)` — falla al insertar duplicado

### Aislamiento multi-tenant
- Producto creado en tenantA → NO visible en tenantB (mismo slug)
- Query de admin con tenantA → solo retorna productos de tenantA
- Global Query Filter activo → sin filtro manual en tests

### API (WebApplicationFactory)
- `POST /api/products` sin token → 401
- `POST /api/products` con token sin `catalog.write` → 403
- `POST /api/products` con token válido → 201
- `GET /api/public/products` sin token → 200 (anónimo)
- Concurrencia: dos `PUT` simultáneos → uno retorna 409 (RowVersion)

### Webhooks de pago
- Webhook duplicado → segundo procesamiento retorna 200 sin cambiar estado
- Webhook con firma inválida → 401
- Webhook válido Captured → Order.Status = Paid

## Reglas de tests de integración

```csharp
// Setup: una DB por clase de test
// Reset: Respawn antes de cada método [Fact]
// Tenants: usar Guid.NewGuid() para TenantId en cada test (aislamiento)
// No compartir estado entre tests
```

## Cobertura mínima requerida

| Módulo | Cobertura mínima |
|---|---|
| Domain (Products) | 90% |
| Application validators | 85% |
| Application handlers | 80% |
| Integration (endpoints) | Casos críticos (ver `critical-test-cases.md`) |

## Notas operativas reales (no en el diseño original)

- **`EShopy.Tests.Integration` corre sin paralelizar** (`[assembly: CollectionBehavior(DisableTestParallelization = true)]`
  en `AssemblyInfo.cs`). Encontrado 2026-07-26 al agregar el chequeo de migraciones pendientes de
  B-02: `WebApplicationFactory<Program>` usa `HostFactoryResolver` para interceptar `Build()` del
  host antes de que corra, y ese mecanismo no es seguro para invocaciones concurrentes en el mismo
  proceso — cada clase de test crea su propia factory (`IClassFixture`, no compartida), y sin esto
  fallaban de forma intermitente con `"The entry point exited without ever building an IHost"`. Si
  se necesita paralelizar en el futuro, investigar `ICollectionFixture` compartido en vez de
  revertir esto.
- **Toda clase de test de integración debe usar `SecurityWebApplicationFactory`** (o una factory que
  llame `builder.UseEnvironment("Testing")`), nunca `WebApplicationFactory<Program>` a secas — el
  ambiente "Development" dispara el chequeo de migraciones de B-02 en `Program.cs`, que hace una
  llamada real a SQL Server. `HealthEndpointTests` uso la factory generica originalmente y rompio
  exactamente por esto la primera vez que hubo una migracion pendiente sin aplicar.
- Real de EF/SQL Server para tests de integración: no hay Testcontainers ni Respawn implementados
  todavía (a pesar de figurar en "Herramientas" arriba) — lo que existe hoy son fakes en memoria
  (`EShopy.Tests.Integration/Support/InMemory*.cs`) registrados en `SecurityWebApplicationFactory`,
  uno por cada repositorio/writer. La DB real solo se toca en el smoke test manual contra Docker
  (ver `CURRENT_STATE.md`), no en `dotnet test`.

## CI (pendiente configurar)

```yaml
# .github/workflows/ci.yml (diseño)
- name: Unit Tests
  run: dotnet test EShopy.Tests.Unit

- name: Integration Tests
  run: dotnet test EShopy.Tests.Integration
  # Requiere: SQL Server o Testcontainers disponible
```
