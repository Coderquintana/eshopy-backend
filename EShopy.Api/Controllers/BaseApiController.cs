using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers;

[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
  [NonAction]
  protected string GetCorrelationId()
    => HttpContext.Items.TryGetValue("X-Correlation-Id", out var v) ? v?.ToString() ?? "" : "";

  [NonAction]
  protected string GetTraceId() => HttpContext.TraceIdentifier;

  [NonAction]
  protected string? GetUserId()
    => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

  [NonAction]
  protected string? GetUsername()
    => User.FindFirstValue("preferred_username") ?? User.Identity?.Name;

  [NonAction]
  protected IReadOnlyList<string> GetRoles()
    => User.FindAll("roles").Select(c => c.Value).ToList();
}
