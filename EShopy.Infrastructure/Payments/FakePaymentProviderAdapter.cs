using System.Text.Json;
using System.Text.Json.Serialization;
using EShopy.Application.Common.Payments;

namespace EShopy.Infrastructure.Payments;

/// <summary>
/// Adapter dev-only, siempre exitoso — permite probar el flujo completo de checkout + webhook sin
/// credenciales reales de Bancard/PagoPar. Se reemplaza por un adapter real cuando exista
/// documentacion de esos providers (ver domain/payments.md).
///
/// El formato del webhook (header de firma, forma del payload) es INVENTADO por este adapter — no
/// es el formato real de ningun provider. Sirve para ejercitar el codigo real del endpoint de
/// webhook (validacion de firma, parseo, idempotencia) en dev/tests sin esperar la documentacion de
/// Bancard/PagoPar. Cuando esos adapters existan, cada uno interpreta la firma/payload real de su
/// propio provider — este adapter no cambia.
/// </summary>
public sealed class FakePaymentProviderAdapter : IPaymentProviderAdapter
{
  /// <summary>Secret fijo de dev — sin ningun valor de seguridad real, solo para ejercitar el codigo de validacion.</summary>
  public const string WebhookSignatureHeader = "X-Fake-Signature";
  public const string WebhookSecret = "fake-webhook-secret-dev-only";

  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public string Provider => "fake";

  public Task<InitiatePaymentResult> InitiateAsync(InitiatePaymentRequest request, CancellationToken ct)
  {
    var providerPaymentId = $"fake-payment-{Guid.NewGuid():N}";
    var paymentUrl = $"https://fake-payment.local/pay/{providerPaymentId}";

    return Task.FromResult(new InitiatePaymentResult(providerPaymentId, paymentUrl));
  }

  public bool ValidateWebhookSignature(string rawBody, IReadOnlyDictionary<string, string> headers)
    => headers.TryGetValue(WebhookSignatureHeader, out var signature) && signature == WebhookSecret;

  public WebhookEvent ParseWebhook(string rawBody)
  {
    var payload = JsonSerializer.Deserialize<FakeWebhookPayload>(rawBody, JsonOptions)
      ?? throw new JsonException("Payload de webhook fake vacio o invalido.");

    return new WebhookEvent(payload.EventId, payload.ProviderPaymentId, payload.EventType);
  }

  /// <summary>Forma propia de este adapter dev-only — no la de ningun provider real.</summary>
  private sealed class FakeWebhookPayload
  {
    public required string EventId { get; init; }
    public required string ProviderPaymentId { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PaymentWebhookEventType EventType { get; init; }
  }
}
