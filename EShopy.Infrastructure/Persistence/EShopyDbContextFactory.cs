using EShopy.Application.Common.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EShopy.Infrastructure.Persistence;

/// <summary>
/// Factory para design-time (migrations). Provee un EShopyDbContext sin TenantId activo
/// para que EF Tools pueda ejecutar comandos sin un request HTTP real.
/// </summary>
public sealed class EShopyDbContextFactory : IDesignTimeDbContextFactory<EShopyDbContext>
{
  public EShopyDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<EShopyDbContext>();
    var connectionString = "Server=localhost,1433;Database=EShopy.Dev;User Id=sa;Password=EShopy_Dev_2026!;TrustServerCertificate=True;";
    optionsBuilder.UseSqlServer(connectionString);

    // TenantContext vacío: TenantId = null → el Global Query Filter es transparente en design-time
    return new EShopyDbContext(optionsBuilder.Options, new TenantContext());
  }
}
