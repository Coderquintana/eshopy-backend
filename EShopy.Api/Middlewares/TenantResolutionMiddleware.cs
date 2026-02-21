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
    var subdomain = ExtractSubdomain(host);

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

  private static string ExtractSubdomain(string host)
  {
    // Ejemplos:
    // demo.eshopy.com.py => demo
    // admin.demo.eshopy.com.py => demo (admin es prefijo)
    // localhost => localhost (dev)
    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
      return "localhost";

    var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length < 3) // heuristic
      return parts.FirstOrDefault() ?? "";

    // Si empieza con admin, usar el siguiente como tenant
    if (string.Equals(parts[0], "admin", StringComparison.OrdinalIgnoreCase) && parts.Length >= 2)
      return parts[1];

    return parts[0];
  }
}
