using EShopy.Application.Carts;
using EShopy.Application.Carts.Commands;
using EShopy.Application.Carts.Queries;
using EShopy.Application.Common.Identity;
using EShopy.Application.Common.Stores;
using EShopy.Application.Common.Tenants;
using EShopy.Application.Products;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Queries;
using EShopy.Application.Subscriptions;
using EShopy.Application.Tenants;
using EShopy.Application.Tenants.Commands;
using EShopy.Application.Tenants.Queries;
using EShopy.Infrastructure.Carts;
using EShopy.Infrastructure.Identity;
using EShopy.Infrastructure.Persistence;
using EShopy.Infrastructure.Products;
using EShopy.Infrastructure.Stores;
using EShopy.Infrastructure.Subscriptions;
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
    services.AddMemoryCache();

    // Tenant
    services.AddScoped<ITenantResolver, EfTenantResolver>();
    services.AddScoped<ITenantRepository, EfTenantRepository>();
    services.AddScoped<ITenantUserRepository, EfTenantUserRepository>();
    services.AddScoped<ITenantOnboardingWriter, EfTenantOnboardingWriter>();
    services.AddScoped<ITenantActivationWriter, EfTenantActivationWriter>();
    services.AddHttpClient<IKeycloakUserProvisioner, KeycloakAdminClient>();

    // Store
    services.AddScoped<IStoreRepository, EfStoreRepository>();
    services.AddScoped<IStoreService, EfStoreService>();

    // Subscriptions
    services.AddScoped<ISubscriptionRepository, EfSubscriptionRepository>();

    // Repositorios — Catalog
    services.AddScoped<IProductRepository, EfProductRepository>();

    // Carts
    services.AddScoped<ICartRepository, EfCartRepository>();

    // Handlers — Tenants / Store
    services.AddScoped<CreateTenantCommandHandler>();
    services.AddScoped<ActivateTenantCommandHandler>();
    services.AddScoped<UpdateStoreCommandHandler>();
    services.AddScoped<GetStoreQueryHandler>();
    services.AddScoped<GetTenantByIdQueryHandler>();
    services.AddScoped<InviteTenantUserCommandHandler>();
    services.AddScoped<GetTenantUsersQueryHandler>();

    // Handlers de productos — Commands
    services.AddScoped<CreateProductCommandHandler>();
    services.AddScoped<UpdateProductCommandHandler>();
    services.AddScoped<ChangeProductStatusCommandHandler>();

    // Handlers de productos — Queries
    services.AddScoped<GetProductsQueryHandler>();
    services.AddScoped<GetProductByIdQueryHandler>();
    services.AddScoped<GetPublicProductsQueryHandler>();
    services.AddScoped<GetProductBySlugQueryHandler>();

    // Handlers — Cart
    services.AddScoped<AddCartItemCommandHandler>();
    services.AddScoped<UpdateCartItemQuantityCommandHandler>();
    services.AddScoped<RemoveCartItemCommandHandler>();
    services.AddScoped<GetCartQueryHandler>();

    return services;
  }
}
