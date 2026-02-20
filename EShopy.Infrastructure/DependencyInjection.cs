using EShopy.Application.Common.Tenants;
using EShopy.Application.Products;
using EShopy.Infrastructure.Persistence;
using EShopy.Infrastructure.Products;
using EShopy.Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EShopy.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
      throw new InvalidOperationException("Connection string 'DefaultConnection' no configurada.");

    services.AddDbContext<EShopyDbContext>(options => options.UseSqlServer(connectionString));
    services.AddSingleton<ITenantResolver, InMemoryTenantResolver>();
    services.AddScoped<IProductRepository, EfProductRepository>();
    return services;
  }
}
