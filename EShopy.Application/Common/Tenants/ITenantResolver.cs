namespace EShopy.Application.Common.Tenants;

public interface ITenantResolver
{
  Task<TenantResolution?> ResolveAsync(string subdomain, CancellationToken ct);
}
