using EShopy.Application.Common.Context;
using EShopy.Domain.Carts;
using EShopy.Domain.Products;
using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;
using EShopy.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

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

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfiguration(new ProductConfiguration());
    modelBuilder.ApplyConfiguration(new TenantConfiguration());
    modelBuilder.ApplyConfiguration(new StoreConfiguration());
    modelBuilder.ApplyConfiguration(new TenantUserConfiguration());
    modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
    modelBuilder.ApplyConfiguration(new CartConfiguration());
    modelBuilder.ApplyConfiguration(new CartItemConfiguration());

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
  }
}
