namespace EShopy.Application.Common.Payments;

/// <summary>
/// Adaptador de un provider de pago externo (Bancard/PagoPar). Solo InitiateAsync por ahora — los
/// metodos de webhook (ValidateWebhookSignature/ParseWebhookAsync) se agregan cuando se construya
/// ese modulo, no antes: el diseño original los incluia aca, pero agregarlos sin un caller real
/// hoy seria la misma trampa de "construir antes de necesitarlo" que ya evitamos con IUnitOfWork.
/// Nota de diseño: tampoco toma tipos de ASP.NET Core (HttpRequest) — Application no depende del
/// framework web, igual que el resto del proyecto.
/// </summary>
public interface IPaymentProviderAdapter
{
  string Provider { get; }

  Task<InitiatePaymentResult> InitiateAsync(InitiatePaymentRequest request, CancellationToken ct);
}

/// <summary>OrderReference es Order.Id (Guid) — no OrderNumber, que todavia no existe en este punto del flujo.</summary>
public sealed record InitiatePaymentRequest(Guid OrderReference, decimal Amount, string CurrencyCode);

public sealed record InitiatePaymentResult(string ProviderPaymentId, string PaymentUrl);
