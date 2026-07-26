using EShopy.Application.Tenants;
using EShopy.Domain.Tenants;

namespace EShopy.Tests.Integration.Support;

internal sealed class InMemoryStoreRepository(InMemoryTenantsState state) : IStoreRepository
{
  public Task<Store?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct)
    => Task.FromResult(state.StoresByTenantId.GetValueOrDefault(tenantId));

  public Task UpdateAsync(Store store, CancellationToken ct)
  {
    state.StoresByTenantId[store.TenantId] = store;
    return Task.CompletedTask;
  }
}
