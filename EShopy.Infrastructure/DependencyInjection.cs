using EShopy.Application.Common.Stores;
using EShopy.Application.Common.Tenants;
using EShopy.Application.Products;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Queries;
using EShopy.Infrastructure.Persistence;
using EShopy.Infrastructure.Products;
using EShopy.Infrastructure.Stores;
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

    // Tenant
    services.AddSingleton<ITenantResolver, InMemoryTenantResolver>();

    // Repositorios
    services.AddScoped<IProductRepository, EfProductRepository>();

    // Store (placeholder hasta implementar el módulo Stores)
    services.AddScoped<IStoreService, InMemoryStoreService>();

    // Handlers de productos — Commands
    services.AddScoped<CreateProductCommandHandler>();
    services.AddScoped<UpdateProductCommandHandler>();
    services.AddScoped<ChangeProductStatusCommandHandler>();

    // Handlers de productos — Queries
    services.AddScoped<GetProductsQueryHandler>();
    services.AddScoped<GetProductByIdQueryHandler>();
    services.AddScoped<GetPublicProductsQueryHandler>();
    services.AddScoped<GetProductBySlugQueryHandler>();

    return services;
  }
}
