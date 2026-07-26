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

    return MapError(result.Code!, result.Message!);
  }

  /// <summary>Overload para Result sin valor (ej. el webhook de pagos, que solo responde 200 OK).</summary>
  [NonAction]
  protected ActionResult FromResult(Result result)
  {
    if (result.IsSuccess)
      return Ok();

    return MapError(result.Code!, result.Message!);
  }

  private ActionResult MapError(string code, string message)
  {
    var error = new ErrorResponse
    {
      TraceId = HttpContext.TraceIdentifier,
      Code = code,
      Message = message
    };

    return code switch
    {
      ErrorCodes.NotFound => NotFound(error),
      ErrorCodes.Conflict => Conflict(error),
      ErrorCodes.ValidationError => BadRequest(error),
      ErrorCodes.Unauthorized => Unauthorized(error),
      ErrorCodes.Forbidden => Forbid(),
      ErrorCodes.TenantNotFound => NotFound(error),
      ErrorCodes.ProductInvalidState => Conflict(error),
      ErrorCodes.ProductNotAvailable => Conflict(error),
      ErrorCodes.OrderInvalidState => Conflict(error),
      ErrorCodes.ConcurrencyConflict => Conflict(error),
      ErrorCodes.TenantInvalidState => Conflict(error),
      ErrorCodes.TenantSuspended => StatusCode(StatusCodes.Status403Forbidden, error),
      ErrorCodes.TenantCancelled => StatusCode(StatusCodes.Status403Forbidden, error),
      ErrorCodes.PaymentWebhookInvalid => Unauthorized(error),
      ErrorCodes.ExternalServiceError => StatusCode(StatusCodes.Status502BadGateway, error),
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
