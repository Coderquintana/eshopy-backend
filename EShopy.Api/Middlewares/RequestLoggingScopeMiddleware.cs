using EShopy.Application.Common.Context;
using EShopy.Infrastructure.Identity;
using Serilog.Context;

namespace EShopy.Api.Middlewares;

/// <summary>
/// Enriquece TODOS los logs de la request (los nuestros y los de ASP.NET Core/EF Core) con contexto
/// de tenant/usuario/correlacion, via Serilog.Context.LogContext — requiere Enrich.FromLogContext()
/// en la configuracion de Serilog (ver Program.cs).
/// </summary>
public sealed class RequestLoggingScopeMiddleware(RequestDelegate next)
{
  public async Task Invoke(HttpContext ctx, TenantContext tenant, UserContextAccessor userContextAccessor)
  {
    var user = userContextAccessor.GetUserContext();
    var correlationId = ctx.Items.TryGetValue("X-Correlation-Id", out var cidObj) ? cidObj?.ToString() : null;

    using (LogContext.PushProperty("TenantId", tenant.TenantId))
    using (LogContext.PushProperty("Subdomain", tenant.Subdomain))
    using (LogContext.PushProperty("UserId", user.UserId))
    using (LogContext.PushProperty("UserEmail", user.Email))
    using (LogContext.PushProperty("UserDisplayName", user.DisplayName))
    using (LogContext.PushProperty("UserRoles", string.Join(",", user.Roles)))
    using (LogContext.PushProperty("UserPermissions", string.Join(",", user.Permissions)))
    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("TraceId", ctx.TraceIdentifier))
    using (LogContext.PushProperty("RequestPath", ctx.Request.Path.ToString()))
    using (LogContext.PushProperty("RequestMethod", ctx.Request.Method))
    {
      await next(ctx);
    }
  }
}
