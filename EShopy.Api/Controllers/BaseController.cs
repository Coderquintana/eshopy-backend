using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
  [NonAction]
  protected string GetCorrelationId()
    => HttpContext.Items.TryGetValue("X-Correlation-Id", out var v) ? v?.ToString() ?? "" : "";

  [NonAction]
  protected string GetTraceId() => HttpContext.TraceIdentifier;
}
