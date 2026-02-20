using EShopy.Domain.Products;
using EShopy.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Persistence;

public sealed class EShopyDbContext(DbContextOptions<EShopyDbContext> options) : DbContext(options)
{
  public DbSet<Product> Products => Set<Product>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfiguration(new ProductConfiguration());
  }
}
