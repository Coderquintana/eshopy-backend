using EShopy.Application.Common.Context;
using EShopy.Domain.Carts;
using EShopy.Domain.Common.Audit;
using EShopy.Domain.Common.Counters;
using EShopy.Domain.Orders;
using EShopy.Domain.Payments;
using EShopy.Domain.Products;
using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;
using EShopy.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EShopy.Infrastructure.Persistence;

public sealed class EShopyDbContext(
  DbContextOptions<EShopyDbContext> options,
  TenantContext tenantContext) : DbContext(options)
{
  public DbSet<Product> Products => Set<Product>();

  /// <summary>Global: no lleva TenantId, no participa del Global Query Filter.</summary>
  public DbSet<Tenant> Tenants => Set<Tenant>();

  public DbSet<Store> Stores => Set<Store>();
  public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
  public DbSet<Subscription> Subscriptions => Set<Subscription>();
  public DbSet<Cart> Carts => Set<Cart>();
  public DbSet<Order> Orders => Set<Order>();
  public DbSet<Payment> Payments => Set<Payment>();
  public DbSet<TenantCounter> TenantCounters => Set<TenantCounter>();

  /// <summary>Global: ledger de idempotencia de webhooks, no tiene TenantId (ver PaymentEventProcessed).</summary>
  public DbSet<PaymentEventProcessed> PaymentEventsProcessed => Set<PaymentEventProcessed>();

  public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfiguration(new ProductConfiguration());
    modelBuilder.ApplyConfiguration(new TenantConfiguration());
    modelBuilder.ApplyConfiguration(new StoreConfiguration());
    modelBuilder.ApplyConfiguration(new TenantUserConfiguration());
    modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
    modelBuilder.ApplyConfiguration(new CartConfiguration());
    modelBuilder.ApplyConfiguration(new CartItemConfiguration());
    modelBuilder.ApplyConfiguration(new OrderConfiguration());
    modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
    modelBuilder.ApplyConfiguration(new PaymentConfiguration());
    modelBuilder.ApplyConfiguration(new TenantCounterConfiguration());
    modelBuilder.ApplyConfiguration(new PaymentEventProcessedConfiguration());
    modelBuilder.ApplyConfiguration(new AuditLogConfiguration());

    // Las PK Guid las asigna el DOMINIO (todas las factories hacen Guid.NewGuid()), no la base.
    //
    // Sin esto EF las trata como store-generated, y entonces usa el valor de la PK para decidir el
    // estado de una entidad que descubre dentro de un agregado ya trackeado: PK en default = Added,
    // PK con valor = Modified. Como el dominio ya asigno el Guid, EF concluye que la fila existe y
    // emite UPDATE en vez de INSERT, que afecta 0 filas y termina en DbUpdateConcurrencyException.
    //
    // Bug real (2026-09-06): agregar un SEGUNDO producto distinto a un carrito ya persistido
    // respondia 409. No se veia al crear el carrito (db.Carts.Add marca el grafo entero como Added)
    // ni al acumular el mismo producto (muta un CartItem ya trackeado). Order/OrderItem tenia la
    // misma bomba sin estallar: hoy los pedidos se crean completos en el checkout y nunca reciben un
    // item despues. Por eso la convencion va global y no en CartItemConfiguration.
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      var key = entityType.FindPrimaryKey();
      if (key is null) continue;

      foreach (var property in key.Properties)
      {
        if (property.ClrType == typeof(Guid))
          property.ValueGenerated = ValueGenerated.Never;
      }
    }

    // Global Query Filter de multi-tenancy.
    // Si TenantId no está disponible (e.g. migrations en design-time, o rutas SUPERADMIN excluidas
    // de TenantResolutionMiddleware), el filtro es transparente.
    // Comparar el Guid? directamente (sin ".Value") es deliberado: EF Core evalua los parametros de
    // un query filter de forma ansiosa al armar el SQL, incluso en la rama del "||" que la logica
    // nunca deberia alcanzar — "!x.HasValue || y == x.Value" tira InvalidOperationException
    // ("Nullable object must have a value") apenas TenantId es null, porque ".Value" se evalua igual.
    // "y == x" (Guid == Guid?) no llama a ".Value" nunca, así que es seguro con TenantId null.
    // Tenant queda afuera: es la entidad global que resuelve el TenantId, no tiene uno propio.
    modelBuilder.Entity<Product>()
      .HasQueryFilter(p => tenantContext.TenantId == null || p.TenantId == tenantContext.TenantId);

    modelBuilder.Entity<Store>()
      .HasQueryFilter(s => tenantContext.TenantId == null || s.TenantId == tenantContext.TenantId);

    modelBuilder.Entity<TenantUser>()
      .HasQueryFilter(u => tenantContext.TenantId == null || u.TenantId == tenantContext.TenantId);

    modelBuilder.Entity<Subscription>()
      .HasQueryFilter(s => tenantContext.TenantId == null || s.TenantId == tenantContext.TenantId);

    modelBuilder.Entity<Cart>()
      .HasQueryFilter(c => tenantContext.TenantId == null || c.TenantId == tenantContext.TenantId);

    // Sin filtro: el webhook de pagos busca Payment por (Provider, ProviderPaymentId) sin tenant
    // conocido de antemano (ver domain/payments.md). Con TenantId null el filtro es transparente,
    // asi que Order y Payment SI participan del filtro — el webhook simplemente corre con
    // TenantContext.TenantId == null hasta encontrar el Payment y recien ahi fija el tenant.
    modelBuilder.Entity<Order>()
      .HasQueryFilter(o => tenantContext.TenantId == null || o.TenantId == tenantContext.TenantId);

    modelBuilder.Entity<Payment>()
      .HasQueryFilter(p => tenantContext.TenantId == null || p.TenantId == tenantContext.TenantId);

    modelBuilder.Entity<TenantCounter>()
      .HasQueryFilter(c => tenantContext.TenantId == null || c.TenantId == tenantContext.TenantId);

    // AuditLog.TenantId es nullable (hay entradas a nivel plataforma) — mismo patron seguro, ahora
    // Guid? de los dos lados: EF Core traduce esto a una comparacion SQL null-safe.
    modelBuilder.Entity<AuditLog>()
      .HasQueryFilter(a => tenantContext.TenantId == null || a.TenantId == tenantContext.TenantId);
  }
}
