# Architecture — Database Schema

> SQL Server. EF Core 10. Todas las entidades multi-tenant heredan de AppEntity.

## Columnas base (AppEntity)

Todas las tablas multi-tenant incluyen estas columnas:

| Columna | Tipo SQL | Nullable | Descripción |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK, generado en dominio |
| `TenantId` | `uniqueidentifier` | No | FK al tenant. Global Query Filter obligatorio |
| `CreatedAtUtc` | `datetime2` | No | Fecha de creación UTC |
| `CreatedBy` | `nvarchar(100)` | Sí | Username del creador |
| `UpdatedAtUtc` | `datetime2` | Sí | Última modificación UTC |
| `UpdatedBy` | `nvarchar(100)` | Sí | Username del modificador |
| `RowVersion` | `rowversion` | No | Concurrencia optimista (ETag) |
| `Data` | `nvarchar(max)` | Sí | JSON para extensiones tipadas |

> `HasComment()` obligatorio en cada columna en la configuración EF Core.

## Tablas principales (MVP)

| Tabla | Global/MT | Estado |
|---|---|---|
| `Tenants` | Global | ✅ Migración creada (`AddTenantsStoresSubscriptions`) |
| `Stores` | Multi-tenant | ✅ Migración creada |
| `Subscriptions` | Multi-tenant | ✅ Migración creada |
| `TenantUsers` | Multi-tenant | ✅ Migración creada |
| `Products` | Multi-tenant | ✅ Migración creada |
| `ProductImages` | Multi-tenant | ❌ Pendiente |
| `Carts` | Multi-tenant | ✅ Migración creada (`AddCartsCartItems`) |
| `CartItems` | Multi-tenant (no AppEntity) | ✅ Migración creada |
| `Orders` | Multi-tenant | ✅ Migración creada (`AddOrdersPaymentsTenantCounters`) |
| `OrderItems` | Multi-tenant (no AppEntity) | ✅ Migración creada |
| `Payments` | Multi-tenant | ✅ Migración creada |
| `PaymentEventsProcessed` | Global (sin `TenantId`) | ✅ Migración creada (`AddPaymentEventsProcessed`) |
| `TenantCounters` | Multi-tenant (PK compuesta, sin `Id`) | ✅ Migración creada |
| `AuditLogs` | Multi-tenant | ❌ Pendiente |

## Tabla: Products (implementada)

| Columna | Tipo | Nullable | Constraint |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `TenantId` | `uniqueidentifier` | No | FK a Tenants (`FK_Products_Tenants_TenantId`) |
| `StoreId` | `uniqueidentifier` | No | FK a Stores (`FK_Products_Stores_StoreId`) |
| `Slug` | `nvarchar(200)` | No | — |
| `Sku` | `nvarchar(64)` | Sí | — |
| `Name` | `nvarchar(300)` | No | — |
| `Description` | `nvarchar(max)` | Sí | — |
| `Price` | `decimal(18,2)` | No | CHECK >= 0 |
| `CurrencyCode` | `nvarchar(3)` | No | — |
| `Status` | `int` | No | 0=Draft, 1=Active, 2=Archived |
| `StockOnHand` | `int` | No | CHECK >= 0 |
| + columnas AppEntity | | | Ver tabla AppEntity arriba |

### Índices de Products

| Nombre | Columnas | Tipo |
|---|---|---|
| `UQ_Products_TenantId_Slug` | `(TenantId, Slug)` | UNIQUE |
| `UQ_Products_TenantId_Sku` | `(TenantId, Sku)` WHERE Sku IS NOT NULL | UNIQUE filtrado |
| `IX_Products_TenantId_Status` | `(TenantId, Status)` | IX |
| `IX_Products_TenantId_Name` | `(TenantId, Name)` | IX |

## Tabla: Tenants (implementada)

Entidad global: no lleva `TenantId` ni las columnas de `AppEntity` (no participa del Global Query Filter).

| Columna | Tipo | Nullable | Constraint |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `Subdomain` | `nvarchar(50)` | No | UNIQUE global |
| `BusinessName` | `nvarchar(200)` | No | — |
| `Status` | `tinyint` | No | 0=PendingPayment, 1=Active, 2=Suspended, 3=Cancelled |
| `Plan` | `tinyint` | No | 0=Basic, 1=Gold, 2=Diamond |
| `CreatedAtUtc` | `datetime2` | No | — |
| `UpdatedAtUtc` | `datetime2` | Sí | — |
| `ActivatedAtUtc` | `datetime2` | Sí | Fecha de la primera activación |

Índice: `UQ_Tenants_Subdomain` (`Subdomain`, UNIQUE).

## Tabla: Stores (implementada)

| Columna | Tipo | Nullable | Constraint |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `Name` | `nvarchar(200)` | No | — |
| `CurrencyCode` | `char(3)` | No | Inmutable tras creación |
| `Timezone` | `nvarchar(100)` | No | Default `America/Asuncion` |
| `PrimaryColor` / `BackgroundColor` | `nvarchar(7)` | Sí | Hex color |
| `LogoUrl` | `nvarchar(500)` | Sí | — |
| `Description` | `nvarchar(1000)` | Sí | — |
| + columnas AppEntity | | | Ver tabla AppEntity arriba |

Índice: `UQ_Stores_TenantId` (`TenantId`, UNIQUE — 1:1 con Tenant en MVP).

## Tabla: TenantUsers (implementada)

| Columna | Tipo | Nullable | Constraint |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `KeycloakUserId` | `nvarchar(100)` | No | — |
| `Email` | `nvarchar(200)` | No | UNIQUE por tenant |
| `Name` | `nvarchar(200)` | No | — |
| `Role` | `tinyint` | No | 0=Owner, 1=Admin, 2=Staff |
| `IsActive` | `bit` | No | — |
| + columnas AppEntity | | | Ver tabla AppEntity arriba |

Índice: `UQ_TenantUsers_TenantId_Email` (`TenantId`, `Email`, UNIQUE).

## Tabla: Subscriptions (implementada)

| Columna | Tipo | Nullable | Constraint |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `Plan` | `tinyint` | No | 0=Basic, 1=Gold, 2=Diamond |
| `Status` | `tinyint` | No | 0=PendingActivation, 1=Active, 2=PastDue, 3=Suspended, 4=Cancelled |
| `BillingCycleStart` / `BillingCycleEnd` | `datetime2` | No | Iguales mientras `Status = PendingActivation` (aun no hay ciclo real) |
| `PriceAmount` | `decimal(18,2)` | No | CHECK >= 0. Hoy siempre 0 — precios reales TBD, ver `domain/subscriptions.md` |
| `CurrencyCode` | `char(3)` | No | — |
| `ExternalSubscriptionId` | `nvarchar(100)` | Sí | Id en billing externo (Fase 8) |
| `CancelledAtUtc` | `datetime2` | Sí | — |
| + columnas AppEntity | | | Ver tabla AppEntity arriba |

Índice: `UQ_Subscriptions_TenantId_NonCancelled` (`TenantId`, UNIQUE filtrado `WHERE [Status] <> 4`) —
enforced a nivel DB: no puede haber mas de una suscripcion no cancelada por tenant.

## Tabla: Orders (implementada)

| Columna | Tipo | Nullable | Notas |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `TenantId` | `uniqueidentifier` | No | — |
| `StoreId` | `uniqueidentifier` | No | FK a Stores (`FK_Orders_Stores_StoreId`) |
| `OrderNumber` | `int` | No | Secuencial por tenant, asignado por `ICheckoutWriter`. UNIQUE con TenantId |
| `Status` | `tinyint` | No | 0=PendingPayment, 1=Paid, 2=Cancelled, 3=Refunded |
| `BuyerEmail` | `nvarchar(200)` | No | — |
| `BuyerName` | `nvarchar(200)` | No | — |
| `ShippingAddress` | `nvarchar(1000)` | Sí | — |
| `TotalAmount` | `decimal(18,2)` | No | CHECK >= 0. Snapshot, suma de OrderItems |
| `CurrencyCode` | `char(3)` | No | — |
| `CartToken` | `nvarchar(100)` | No | — |
| `PaymentId` | `uniqueidentifier` | Sí | **Sin FK enforced** a proposito — la FK real vive en `Payments.OrderId` (ver tabla abajo). Un FK real en ambas direcciones seria circular, EF no puede resolver el orden de insercion en un solo `SaveChangesAsync` |
| + columnas AppEntity | | | — |

### Índices de Orders

| Índice | Tipo |
|---|---|
| `UQ_Orders_TenantId_OrderNumber` (`TenantId`, `OrderNumber`) | UNIQUE |
| `IX_Orders_TenantId_Status` | IX |
| `IX_Orders_TenantId_BuyerEmail` | IX |

## Tabla: OrderItems (implementada)

Snapshot inmutable, no hereda `AppEntity` (igual que `CartItems`, se resuelve via `OrderId`).

| Columna | Tipo | Nullable | Notas |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `OrderId` | `uniqueidentifier` | No | FK a Orders, `Cascade` |
| `ProductId` | `uniqueidentifier` | No | FK a Products, `Restrict` — referencia historica |
| `ProductName` | `nvarchar(300)` | No | Snapshot al checkout |
| `ProductSku` | `nvarchar(64)` | Sí | Snapshot al checkout |
| `UnitPrice` | `decimal(18,2)` | No | CHECK >= 0. Snapshot al checkout |
| `Quantity` | `int` | No | CHECK >= 1 |

`Subtotal` (`UnitPrice * Quantity`) es calculado en dominio, `Ignore()` en EF — nunca persistido, nunca
puede desincronizarse.

## Tabla: Payments (implementada)

| Columna | Tipo | Nullable | Notas |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `TenantId` | `uniqueidentifier` | No | — |
| `OrderId` | `uniqueidentifier` | No | FK a Orders (`FK_Payments_Orders_OrderId`, `Restrict`) — la direccion real de la relacion circular con `Orders.PaymentId` |
| `Status` | `tinyint` | No | 0=Initiated, 1=Authorized, 2=Captured, 3=Failed, 4=Refunded |
| `Provider` | `nvarchar(50)` | No | `'fake'` hoy — Bancard/PagoPar cuando exista su adapter real |
| `ProviderPaymentId` | `nvarchar(200)` | Sí | — |
| `ProviderPaymentUrl` | `nvarchar(1000)` | Sí | — |
| `Amount` | `decimal(18,2)` | No | CHECK >= 0 |
| `CurrencyCode` | `char(3)` | No | — |
| `ErrorCode` / `ErrorMessage` | `nvarchar(100)` / `nvarchar(1000)` | Sí | — |
| + columnas AppEntity | | | — |

### Índices de Payments

| Índice | Tipo |
|---|---|
| `IX_Payments_OrderId` | IX |
| `IX_Payments_Provider_ProviderPaymentId` (`Provider`, `ProviderPaymentId`) WHERE `ProviderPaymentId IS NOT NULL` | IX filtrado — el webhook lo usa para resolver el Payment sin tenant conocido |

## Tabla: PaymentEventsProcessed (implementada)

> Global, sin `TenantId` ni Global Query Filter: el chequeo de idempotencia ocurre antes de que el
> tenant se conozca, y `(Provider, EventId)` ya es unico a nivel provider. Ver `domain/payments.md`
> "Flujo de webhook idempotente".

| Columna | Tipo | Nullable | Notas |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `Provider` | `nvarchar(50)` | No | — |
| `EventId` | `nvarchar(200)` | No | Id del evento segun el provider |
| `ProcessedAtUtc` | `datetime2` | No | — |

### Índices de PaymentEventsProcessed

| Índice | Tipo |
|---|---|
| `UQ_PaymentEventsProcessed_Provider_EventId` (`Provider`, `EventId`) | UNIQUE |

## Tabla: TenantCounters (para OrderNumber, implementada)

> Sin SQL crudo. Entidad EF normal, atomicidad via concurrency token + reintento — no
> `UPDLOCK`/`ROWLOCK`. Ver `domain/orders.md` "Escritura atomica (ICheckoutWriter)" para el bug real
> de concurrencia encontrado y corregido el 2026-07-26 (el reintento tambien debe atrapar violaciones
> de indice unico, no solo `DbUpdateConcurrencyException`).

| Columna | Tipo | Nullable | Notas |
|---|---|---|---|
| `TenantId` | `uniqueidentifier` | No | PK compuesta (`TenantId`, `CounterType`) |
| `CounterType` | `nvarchar(50)` | No | `'OrderNumber'` (extensible a otros contadores por tenant a futuro) |
| `CurrentValue` | `int` | No | `IsConcurrencyToken()` en la configuracion EF — asi el `UPDATE` generado por EF incluye `WHERE CurrentValue = @valorLeido` automaticamente |

## Tabla: Carts (implementada)

| Columna | Tipo | Nullable | Notas |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `TenantId` | `uniqueidentifier` | No | — |
| `CartToken` | `nvarchar(100)` | No | UUID generado en frontend |
| `ExpiresAtUtc` | `datetime2` | No | Para limpieza de carritos abandonados (F6-04) |
| + columnas AppEntity | | | — |

### Índices de Carts

| Índice | Tipo |
|---|---|
| `(TenantId, CartToken)` | UNIQUE — no `CartToken` solo (ver `domain/carts.md`) |

## Tabla: CartItems (implementada)

| Columna | Tipo | Nullable | Notas |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `CartId` | `uniqueidentifier` | No | FK a Carts |
| `ProductId` | `uniqueidentifier` | No | FK a Products |
| `Quantity` | `int` | No | CHECK >= 1 |
| `CreatedAtUtc` / `UpdatedAtUtc` | `datetime2` | No/Sí | Sin resto de columnas AppEntity — se resuelve via `CartId`, igual que `OrderItem` via `OrderId` |

### Índices de CartItems

| Índice | Tipo |
|---|---|
| `(CartId, ProductId)` | UNIQUE — un producto, una fila (acumula cantidad, no duplica) |

## Convenciones EF Core

```csharp
// Ejemplo de configuración correcta
builder.Property(p => p.Slug)
    .IsRequired()
    .HasMaxLength(200)
    .HasComment("Identificador único legible del producto. Normalizado a minúsculas.");

builder.HasIndex(p => new { p.TenantId, p.Slug })
    .IsUnique()
    .HasDatabaseName("UQ_Products_TenantId_Slug");
```

## Base de datos de desarrollo

- Contenedor SQL Server 2022 via `docker-compose.yml` (ver `docs/keycloak-setup.md` §0), `localhost:1433`
- Base de datos: `EShopy.Dev`
- Auth: SQL (`sa` / password en `MSSQL_SA_PASSWORD`, ver `docker-compose.yml`)

## Migraciones

```bash
# Crear migración
dotnet ef migrations add <NombreMigracion> --project EShopy.Infrastructure --startup-project EShopy.Api

# Aplicar migraciones
dotnet ef database update --project EShopy.Infrastructure --startup-project EShopy.Api
```
