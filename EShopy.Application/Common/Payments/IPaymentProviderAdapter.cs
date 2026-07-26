namespace EShopy.Application.Common.Payments;

/// <summary>
/// Adaptador de un provider de pago externo (Bancard/PagoPar). Puede haber mas de una implementacion
/// registrada a la vez (una por provider soportado) — el caller (ver
/// ProcessPaymentWebhookCommandHandler) resuelve la que corresponda via <see cref="Provider"/>.
/// Nota de diseño: no toma tipos de ASP.NET Core (HttpRequest) — Application no depende del
/// framework web, igual que el resto del proyecto. El controller lee el body/headers crudos y se los
/// pasa al adapter como texto/diccionario; cada adapter interpreta esos datos con el formato propio
/// de su provider.
/// </summary>
public interface IPaymentProviderAdapter
{
  string Provider { get; }

  Task<InitiatePaymentResult> InitiateAsync(InitiatePaymentRequest request, CancellationToken ct);

  /// <summary>Valida la firma/secret del webhook segun la convencion propia del provider.</summary>
  bool ValidateWebhookSignature(string rawBody, IReadOnlyDictionary<string, string> headers);

  /// <summary>
  /// Parsea el payload crudo del webhook al formato interno. Se llama SOLO despues de validar la
  /// firma — no debe usarse para decidir si el webhook es confiable.
  /// </summary>
  WebhookEvent ParseWebhook(string rawBody);
}

/// <summary>OrderReference es Order.Id (Guid) — no OrderNumber, que todavia no existe en este punto del flujo.</summary>
public sealed record InitiatePaymentRequest(Guid OrderReference, decimal Amount, string CurrencyCode);

public sealed record InitiatePaymentResult(string ProviderPaymentId, string PaymentUrl);

/// <summary>Evento de webhook ya normalizado, independiente del formato propio de cada provider.</summary>
public sealed record WebhookEvent(string EventId, string ProviderPaymentId, PaymentWebhookEventType EventType);

public enum PaymentWebhookEventType
{
  Captured,
  Failed,
  Refunded
}
