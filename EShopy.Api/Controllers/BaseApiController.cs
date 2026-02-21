using System.Security.Claims;
using EShopy.Api.Common.Http;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers;

[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
  /// <summary>
  /// Convierte un Result&lt;T&gt; de la capa de aplicación en el ActionResult HTTP correspondiente.
  /// Mapea Result.Code al código de estado HTTP correcto.
  /// </summary>
  [NonAction]
  protected ActionResult<T> FromResult<T>(Result<T> result)
  {
    if (result.IsSuccess)
      return Ok(result.Value);

    var error = new ErrorResponse
    {
      TraceId = HttpContext.TraceIdentifier,
      Code = result.Code!,
      Message = result.Message!
    };

    return result.Code switch
    {
      ErrorCodes.NotFound => NotFound(error),
      ErrorCodes.Conflict => Conflict(error),
      ErrorCodes.ValidationError => BadRequest(error),
      ErrorCodes.Unauthorized => Unauthorized(error),
      ErrorCodes.Forbidden => Forbid(),
      ErrorCodes.TenantNotFound => NotFound(error),
      ErrorCodes.ProductInvalidState => Conflict(error),
      ErrorCodes.ProductNotAvailable => Conflict(error),
      _ => StatusCode(StatusCodes.Status500InternalServerError, error)
    };
  }

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
