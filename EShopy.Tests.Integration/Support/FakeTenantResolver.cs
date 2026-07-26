using EShopy.Application.Common.Tenants;
using EShopy.Domain.Tenants;

namespace EShopy.Tests.Integration.Support;

/// <summary>Resuelve "localhost" a un tenant Active fijo, sin tocar la base de datos.</summary>
internal sealed class FakeTenantResolver : ITenantResolver
{
  public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

  public Task<TenantResolution?> ResolveAsync(string subdomain, CancellationToken ct)
  {
    if (string.Equals(subdomain, "localhost", StringComparison.OrdinalIgnoreCase))
      return Task.FromResult<TenantResolution?>(new TenantResolution(TenantId, TenantStatus.Active));

    return Task.FromResult<TenantResolution?>(null);
  }
}
