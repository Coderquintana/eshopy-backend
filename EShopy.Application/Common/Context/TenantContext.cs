namespace EShopy.Application.Common.Context;

public sealed class TenantContext
{
  public Guid? TenantId { get; private set; }
  public string? Subdomain { get; private set; }

  public void Set(Guid tenantId, string subdomain)
  {
    TenantId = tenantId;
    Subdomain = subdomain;
  }
}
