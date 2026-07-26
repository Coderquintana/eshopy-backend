using EShopy.Application.Tenants;
using EShopy.Domain.Tenants;

namespace EShopy.Tests.Integration.Support;

internal sealed class InMemoryTenantUserRepository(InMemoryTenantsState state) : ITenantUserRepository
{
  public Task<bool> EmailExistsForTenantAsync(Guid tenantId, string email, CancellationToken ct)
    => Task.FromResult(state.TenantUsers.Any(u => u.TenantId == tenantId && u.Email == email));

  public Task<IReadOnlyList<TenantUser>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct)
    => Task.FromResult((IReadOnlyList<TenantUser>)state.TenantUsers.Where(u => u.TenantId == tenantId).ToList());

  public Task AddAsync(TenantUser tenantUser, CancellationToken ct)
  {
    state.TenantUsers.Add(tenantUser);
    return Task.CompletedTask;
  }
}
