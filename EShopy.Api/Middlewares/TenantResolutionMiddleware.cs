using EShopy.Application.Common.Context;
using EShopy.Application.Common.Tenants;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Tenants;

namespace EShopy.Api.Middlewares;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
  // Rutas que no requieren tenant (sin subdominio comercial, u operaciones a nivel plataforma)
  private static readonly string[] ExcludedPrefixes =
  [
    "/health",
    "/swagger",
    "/api/onboarding/tenants",
    "/api/admin/tenants",
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

    var resolution = await tenantResolver.ResolveAsync(subdomain, ctx.RequestAborted);
    if (resolution is null)
      throw new DomainException(ErrorCodes.TenantNotFound, $"Tenant no encontrado para el subdominio '{subdomain}'.");

    switch (resolution.Status)
    {
      case TenantStatus.Suspended:
        throw new DomainException(ErrorCodes.TenantSuspended, "El tenant esta suspendido por falta de pago.");
      case TenantStatus.Cancelled:
        throw new DomainException(ErrorCodes.TenantCancelled, "El tenant fue cancelado.");
    }

    tenantContext.Set(resolution.TenantId, subdomain);

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
