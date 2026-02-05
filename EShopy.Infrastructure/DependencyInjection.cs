using EShopy.Application.Common.Tenants;
using EShopy.Infrastructure.Tenants;
using Microsoft.Extensions.DependencyInjection;

namespace EShopy.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services)
  {
    services.AddSingleton<ITenantResolver, InMemoryTenantResolver>();
    return services;
  }
}
