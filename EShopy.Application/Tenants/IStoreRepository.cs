using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants;

public interface IStoreRepository
{
  Task<Store?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct);
  Task UpdateAsync(Store store, CancellationToken ct);
}
