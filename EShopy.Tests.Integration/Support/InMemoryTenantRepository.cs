using EShopy.Application.Tenants;
using EShopy.Domain.Tenants;

namespace EShopy.Tests.Integration.Support;

internal sealed class InMemoryTenantRepository(InMemoryTenantsState state) : ITenantRepository
{
  public Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken ct)
    => Task.FromResult(state.Tenants.Values.Any(t => t.Subdomain == subdomain));

  public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct)
    => Task.FromResult(state.Tenants.Values.FirstOrDefault(t => t.Subdomain == subdomain));

  public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct)
    => Task.FromResult(state.Tenants.GetValueOrDefault(id));
}
