using EShopy.Application.Common.Context;
using EShopy.Application.Common.Tenants;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Api.Middlewares;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
  // Rutas que no requieren tenant (sin subdominio comercial)
  private static readonly string[] ExcludedPrefixes =
  [
    "/health",
    "/swagger",
    "/api/onboarding/tenants",
    "/api/payments/webhooks"
  ];

  public async Task Invoke(HttpContext ctx, TenantContext tenantContext, ITenantResolver tenantResolver, ILogger<TenantResolutionMiddleware> log)
  {
    // Omitir resolución de tenant para rutas excluidas
    var path = ctx.Request.Path.Value ?? "";
    if (IsExcluded(path))
    {
      await next(ctx);
      return;
    }

    var host = ctx.Request.Host.Host ?? "";
    var subdomain = SubdomainResolver.Extract(host);

    if (string.IsNullOrWhiteSpace(subdomain))
      throw new DomainException(ErrorCodes.TenantNotFound, "Falta el subdominio del tenant.");

    var tenantId = await tenantResolver.ResolveTenantIdAsync(subdomain, ctx.RequestAborted);
    if (tenantId is null)
      throw new DomainException(ErrorCodes.TenantNotFound, $"Tenant no encontrado para el subdominio '{subdomain}'.");

    tenantContext.Set(tenantId.Value, subdomain);

    await next(ctx);
  }

  private static bool IsExcluded(string path)
  {
    foreach (var prefix in ExcludedPrefixes)
    {
      if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        return true;
    }
    return false;
  }
}
