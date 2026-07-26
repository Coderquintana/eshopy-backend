namespace EShopy.Application.Common.Context;

public sealed class TenantContext
{
  public Guid? TenantId { get; private set; }
  public string? Subdomain { get; private set; }

  /// <summary>
  /// <paramref name="subdomain"/> es opcional: el path del webhook de pagos resuelve el tenant por
  /// referencia interna (Provider+ProviderPaymentId), no por Host — no tiene subdominio que fijar.
  /// </summary>
  public void Set(Guid tenantId, string? subdomain = null)
  {
    TenantId = tenantId;
    Subdomain = subdomain;
  }
}
