using EShopy.Application.Common.Context;
using EShopy.Domain.Products;
using EShopy.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Persistence;

public sealed class EShopyDbContext(
  DbContextOptions<EShopyDbContext> options,
  TenantContext tenantContext) : DbContext(options)
{
  public DbSet<Product> Products => Set<Product>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfiguration(new ProductConfiguration());

    // Global Query Filter de multi-tenancy.
    // Si TenantId no está disponible (e.g. migrations en design-time), el filtro es transparente.
    modelBuilder.Entity<Product>()
      .HasQueryFilter(p => !tenantContext.TenantId.HasValue || p.TenantId == tenantContext.TenantId.Value);
  }
}
