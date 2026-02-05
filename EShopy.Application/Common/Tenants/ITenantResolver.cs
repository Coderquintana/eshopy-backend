namespace EShopy.Application.Common.Tenants;

public interface ITenantResolver
{
  Task<Guid?> ResolveTenantIdAsync(string subdomain, CancellationToken ct);
}
