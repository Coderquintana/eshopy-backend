using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EShopy.Infrastructure.Persistence;

public sealed class EShopyDbContextFactory : IDesignTimeDbContextFactory<EShopyDbContext>
{
  public EShopyDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<EShopyDbContext>();
    var connectionString = "Server=lpc:localhost\\SQLEXPRESS;Database=EShopy.Dev;Trusted_Connection=True;TrustServerCertificate=True;";
    optionsBuilder.UseSqlServer(connectionString);

    return new EShopyDbContext(optionsBuilder.Options);
  }
}
