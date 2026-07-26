using EShopy.Application.Common.Payments;

namespace EShopy.Infrastructure.Payments;

/// <summary>
/// Adapter dev-only, siempre exitoso — permite probar el flujo completo de checkout sin
/// credenciales reales de Bancard/PagoPar. Se reemplaza por un adapter real cuando exista
/// documentacion de esos providers (ver domain/payments.md).
/// </summary>
public sealed class FakePaymentProviderAdapter : IPaymentProviderAdapter
{
  public string Provider => "fake";

  public Task<InitiatePaymentResult> InitiateAsync(InitiatePaymentRequest request, CancellationToken ct)
  {
    var providerPaymentId = $"fake-payment-{Guid.NewGuid():N}";
    var paymentUrl = $"https://fake-payment.local/pay/{providerPaymentId}";

    return Task.FromResult(new InitiatePaymentResult(providerPaymentId, paymentUrl));
  }
}
