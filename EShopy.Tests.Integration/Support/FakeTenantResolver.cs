using EShopy.Application.Common.Tenants;
using EShopy.Domain.Tenants;

namespace EShopy.Tests.Integration.Support;

/// <summary>Resuelve dos hosts de tenant Active fijos, sin tocar la base de datos.</summary>
internal sealed class FakeTenantResolver(InMemoryTenantsState state) : ITenantResolver
{
  public Task<TenantResolution?> ResolveAsync(string subdomain, CancellationToken ct)
  {
    var tenant = state.Tenants.Values.FirstOrDefault(item =>
      string.Equals(item.Subdomain, subdomain, StringComparison.OrdinalIgnoreCase));
    if (tenant is not null)
      return Task.FromResult<TenantResolution?>(new TenantResolution(tenant.Id, tenant.Status));

    return Task.FromResult<TenantResolution?>(null);
  }
}
