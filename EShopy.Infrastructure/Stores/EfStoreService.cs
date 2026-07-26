using EShopy.Application.Common.Stores;
using EShopy.Application.Tenants;

namespace EShopy.Infrastructure.Stores;

public sealed class EfStoreService(IStoreRepository repository) : IStoreService
{
  public async Task<StoreDto?> GetDefaultStoreAsync(Guid tenantId, CancellationToken ct)
  {
    var store = await repository.GetByTenantIdAsync(tenantId, ct);
    return store is null ? null : new StoreDto(store.Id, store.CurrencyCode);
  }
}
