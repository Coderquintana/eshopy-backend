using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants;

public interface ITenantUserRepository
{
  Task<bool> EmailExistsForTenantAsync(Guid tenantId, string email, CancellationToken ct);
  Task<IReadOnlyList<TenantUser>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct);
  Task AddAsync(TenantUser tenantUser, CancellationToken ct);
}
