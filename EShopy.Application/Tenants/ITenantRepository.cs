using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants;

public interface ITenantRepository
{
  Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken ct);
  Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct);
  Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct);
}
