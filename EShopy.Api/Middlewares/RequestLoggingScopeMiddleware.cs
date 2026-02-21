using EShopy.Application.Common.Context;
using EShopy.Infrastructure.Identity;

namespace EShopy.Api.Middlewares;

public sealed class RequestLoggingScopeMiddleware(RequestDelegate next)
{
  public async Task Invoke(HttpContext ctx, TenantContext tenant, UserContextAccessor userContextAccessor, ILogger<RequestLoggingScopeMiddleware> log)
  {
    var user = userContextAccessor.GetUserContext();

    var correlationId = ctx.Items.TryGetValue("X-Correlation-Id", out var cidObj) ? cidObj?.ToString() : null;

    using (log.BeginScope(new Dictionary<string, object?>
    {
      ["TenantId"] = tenant.TenantId,
      ["Subdomain"] = tenant.Subdomain,
      ["UserId"] = user.UserId,
      ["UserEmail"] = user.Email,
      ["UserDisplayName"] = user.DisplayName,
      ["UserRoles"] = string.Join(",", user.Roles),
      ["UserPermissions"] = string.Join(",", user.Permissions),
      ["CorrelationId"] = correlationId,
      ["TraceId"] = ctx.TraceIdentifier,
      ["RequestPath"] = ctx.Request.Path.ToString(),
      ["RequestMethod"] = ctx.Request.Method,
    }))
    {
      await next(ctx);
    }
  }
}
