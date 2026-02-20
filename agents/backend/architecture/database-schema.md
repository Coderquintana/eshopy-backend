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
| `Tenants` | Global | ❌ Pendiente |
| `Stores` | Multi-tenant | ❌ Pendiente |
| `Subscriptions` | Multi-tenant | ❌ Pendiente |
| `TenantUsers` | Multi-tenant | ❌ Pendiente |
| `Products` | Multi-tenant | ✅ Migración creada |
| `ProductImages` | Multi-tenant | ❌ Pendiente |
| `Carts` | Multi-tenant | ❌ Pendiente |
| `CartItems` | Multi-tenant | ❌ Pendiente |
| `Orders` | Multi-tenant | ❌ Pendiente |
| `OrderItems` | Multi-tenant (no AppEntity) | ❌ Pendiente |
| `Payments` | Multi-tenant | ❌ Pendiente |
| `PaymentEventsProcessed` | Multi-tenant | ❌ Pendiente |
| `TenantCounters` | Multi-tenant | ❌ Pendiente |
| `AuditLogs` | Multi-tenant | ❌ Pendiente |

## Tabla: Products (implementada)

| Columna | Tipo | Nullable | Constraint |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `TenantId` | `uniqueidentifier` | No | FK (futuro), Index |
| `StoreId` | `uniqueidentifier` | No | FK a Stores (**PENDIENTE** agregar) |
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

## Tabla: Orders (diseño)

| Columna | Tipo | Nullable | Notas |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `TenantId` | `uniqueidentifier` | No | — |
| `StoreId` | `uniqueidentifier` | No | — |
| `OrderNumber` | `int` | No | Secuencial por tenant. UNIQUE con TenantId |
| `Status` | `int` | No | 0=PendingPayment, 1=Paid, 2=Cancelled, 3=Refunded |
| `BuyerEmail` | `nvarchar(200)` | No | — |
| `BuyerName` | `nvarchar(200)` | No | — |
| `TotalAmount` | `decimal(18,2)` | No | Snapshot |
| `CurrencyCode` | `nvarchar(3)` | No | — |
| `CartToken` | `nvarchar(100)` | No | — |
| `PaymentId` | `uniqueidentifier` | Sí | FK a Payments |
| + columnas AppEntity | | | — |

### Índices de Orders

| Índice | Tipo |
|---|---|
| `(TenantId, OrderNumber)` | UNIQUE |
| `(TenantId, Status)` | IX |

## Tabla: TenantCounters (para OrderNumber)

```sql
CREATE TABLE TenantCounters (
    TenantId uniqueidentifier NOT NULL,
    CounterType nvarchar(50) NOT NULL,  -- 'OrderNumber'
    CurrentValue int NOT NULL DEFAULT 0,
    PRIMARY KEY (TenantId, CounterType)
);
```

Uso (debe ser atómico):
```sql
UPDATE TenantCounters WITH (UPDLOCK, ROWLOCK)
SET CurrentValue = CurrentValue + 1
OUTPUT INSERTED.CurrentValue
WHERE TenantId = @tenantId AND CounterType = 'OrderNumber';
```

## Tabla: Carts

| Columna | Tipo | Nullable | Notas |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | PK |
| `TenantId` | `uniqueidentifier` | No | — |
| `CartToken` | `nvarchar(100)` | No | UNIQUE por tenant. UUID generado en frontend |
| `ExpiresAtUtc` | `datetime2` | No | Para limpieza de carritos abandonados |
| + columnas AppEntity | | | — |

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

- Servidor: `localhost\SQLEXPRESS`
- Base de datos: `EShopy.Dev`
- Auth: Windows (Trusted_Connection)
- Si falla conexión: usar `lpc:localhost\SQLEXPRESS` para evitar error de SQL Browser

## Migraciones

```bash
# Crear migración
dotnet ef migrations add <NombreMigracion> --project EShopy.Infrastructure --startup-project EShopy.Api

# Aplicar migraciones
dotnet ef database update --project EShopy.Infrastructure --startup-project EShopy.Api
```
