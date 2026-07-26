using EShopy.Application.Common.Context;
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

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfiguration(new ProductConfiguration());
    modelBuilder.ApplyConfiguration(new TenantConfiguration());
    modelBuilder.ApplyConfiguration(new StoreConfiguration());
    modelBuilder.ApplyConfiguration(new TenantUserConfiguration());
    modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());

    // Global Query Filter de multi-tenancy.
    // Si TenantId no está disponible (e.g. migrations en design-time), el filtro es transparente.
    // Tenant queda afuera: es la entidad global que resuelve el TenantId, no tiene uno propio.
    modelBuilder.Entity<Product>()
      .HasQueryFilter(p => !tenantContext.TenantId.HasValue || p.TenantId == tenantContext.TenantId.Value);

    modelBuilder.Entity<Store>()
      .HasQueryFilter(s => !tenantContext.TenantId.HasValue || s.TenantId == tenantContext.TenantId.Value);

    modelBuilder.Entity<TenantUser>()
      .HasQueryFilter(u => !tenantContext.TenantId.HasValue || u.TenantId == tenantContext.TenantId.Value);

    modelBuilder.Entity<Subscription>()
      .HasQueryFilter(s => !tenantContext.TenantId.HasValue || s.TenantId == tenantContext.TenantId.Value);
  }
}
