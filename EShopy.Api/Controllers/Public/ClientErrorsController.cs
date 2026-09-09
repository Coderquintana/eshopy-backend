using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

/// <summary>Recibe errores no controlados del frontend para diagnóstico operativo.</summary>
[ApiController]
[Route("api/client-errors")]
[AllowAnonymous]
public sealed class ClientErrorsController(ILogger<ClientErrorsController> logger) : ControllerBase
{
  private const int MaxMessageLength = 2000;
  private const int MaxStackLength = 2000;
  private const int MaxUrlLength = 2048;
  private const int MaxUserAgentLength = 512;

  /// <summary>
  /// Registra un error del cliente de forma best-effort. No persiste datos en tablas de negocio.
  /// </summary>
  [HttpPost]
  [RequestSizeLimit(16 * 1024)]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  public IActionResult Report([FromBody] ClientErrorReport report)
  {
    var message = Truncate(report.Message, MaxMessageLength);
    var stack = Truncate(report.Stack, MaxStackLength);
    var url = Truncate(report.Url, MaxUrlLength);
    var userAgent = Truncate(report.UserAgent, MaxUserAgentLength);

    try
    {
      // Template constante a propósito: los valores del navegador quedan como propiedades
      // estructuradas de Serilog, nunca se interpretan como un message template.
      logger.LogError(
        "Client error reported. Message: {ClientMessage}; Stack: {ClientStack}; Url: {ClientUrl}; UserAgent: {ClientUserAgent}",
        message,
        stack,
        url,
        userAgent);
    }
    catch
    {
      // Best-effort: el reporter de errores no debe generar otro error al cliente si falla un sink.
    }

    return NoContent();
  }

  private static string? Truncate(string? value, int maxLength)
  {
    if (value is null || value.Length <= maxLength)
      return value;

    return value[..maxLength];
  }
}

/// <summary>Payload enviado por el GlobalErrorHandler de Admin y Storefront.</summary>
public sealed record ClientErrorReport(
  string Message,
  string? Stack = null,
  string? Url = null,
  string? UserAgent = null);
