using System.Security.Claims;
using EShopy.Application.Common.Context;

namespace EShopy.Api.Middlewares;

public sealed class RequestLoggingScopeMiddleware(RequestDelegate next)
{
  public async Task Invoke(HttpContext ctx, TenantContext tenant, UserContext user, ILogger<RequestLoggingScopeMiddleware> log)
  {
    // Mapear usuario desde claims (si existe)
    var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User.FindFirstValue("sub");
    var email = ctx.User.FindFirstValue(ClaimTypes.Email) ?? ctx.User.FindFirstValue("email");
    var roles = ctx.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray();
    user.Set(userId, email, roles);

    var correlationId = ctx.Items.TryGetValue("X-Correlation-Id", out var cidObj) ? cidObj?.ToString() : null;

    using (log.BeginScope(new Dictionary<string, object?>
    {
      ["TenantId"] = tenant.TenantId,
      ["Subdomain"] = tenant.Subdomain,
      ["UserId"] = user.UserId,
      ["UserEmail"] = user.Email,
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
