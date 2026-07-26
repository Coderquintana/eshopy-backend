using System.Text;
using EShopy.Application.Payments.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

/// <summary>
/// Webhook de confirmacion de pago. Excluido de TenantResolutionMiddleware (el provider no envia un
/// Host que matchee un subdominio nuestro) — el tenant se resuelve dentro del handler, ver
/// ProcessPaymentWebhookCommandHandler.
/// </summary>
[AllowAnonymous]
[Route("api/payments/webhooks")]
public sealed class PaymentsController(ProcessPaymentWebhookCommandHandler webhookHandler) : BaseApiController
{
  /// <summary>Recibe y procesa un evento de webhook de un provider de pago. Siempre lee el body crudo — cada adapter interpreta su propio formato.</summary>
  [HttpPost("{provider}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
  public async Task<ActionResult> Webhook(string provider, CancellationToken ct)
  {
    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
    var rawBody = await reader.ReadToEndAsync(ct);

    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var header in Request.Headers)
      headers[header.Key] = header.Value.ToString();

    var result = await webhookHandler.Handle(new ProcessPaymentWebhookCommand(provider, rawBody, headers), ct);
    return FromResult(result);
  }
}
