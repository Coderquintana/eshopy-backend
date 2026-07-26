using EShopy.Domain.Common.Entities;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Domain.Payments;

/// <summary>
/// Intento de pago asociado a un Order. Se crea en Initiated con lo que devuelve el provider al
/// iniciar el pago; las transiciones siguientes (Authorized/Captured/Failed/Refunded) las dispara
/// el webhook del provider (ver <c>ProcessPaymentWebhookCommandHandler</c>).
/// </summary>
public sealed class Payment : AppEntity
{
  private Payment(Guid id,
    Guid tenantId,
    Guid orderId,
    string provider,
    decimal amount,
    string currencyCode,
    string? providerPaymentId,
    string? providerPaymentUrl,
    DateTime createdAtUtc)
    : base(id, tenantId, createdAtUtc, createdBy: null, createdAtUtc, updatedBy: null, data: null)
  {
    OrderId = orderId;
    Status = PaymentStatus.Initiated;
    Provider = provider;
    Amount = amount;
    CurrencyCode = currencyCode;
    ProviderPaymentId = providerPaymentId;
    ProviderPaymentUrl = providerPaymentUrl;
  }

  public Guid OrderId { get; private set; }
  public PaymentStatus Status { get; private set; }
  public string Provider { get; private set; }
  public string? ProviderPaymentId { get; private set; }
  public string? ProviderPaymentUrl { get; private set; }
  public decimal Amount { get; private set; }
  public string CurrencyCode { get; private set; }
  public string? ErrorCode { get; private set; }
  public string? ErrorMessage { get; private set; }

  public static Payment CreateInitiated(
    Guid tenantId, Guid orderId, string provider, decimal amount, string currencyCode,
    string? providerPaymentId, string? providerPaymentUrl, DateTime createdAtUtc)
  {
    if (string.IsNullOrWhiteSpace(provider))
      throw new DomainException(ErrorCodes.ValidationError, "El provider de pago es obligatorio.");

    return new Payment(Guid.NewGuid(), tenantId, orderId, provider, amount, currencyCode,
      providerPaymentId, providerPaymentUrl, createdAtUtc);
  }

  /// <remarks>
  /// Transiciones validas:
  /// Initiated → Authorized | Initiated → Captured | Initiated → Failed | Authorized → Captured
  /// Authorized → Failed | Captured → Refunded
  ///
  /// Initiated → Captured (agregado con el webhook, Fase 8): varios gateways de redirect no emiten
  /// un evento de autorizacion separado, solo confirman el pago en un unico webhook. Exigir el paso
  /// intermedio Authorized rompería ese caso real sin aportar nada — el estado final es el mismo.
  /// </remarks>
  public void ChangeStatus(PaymentStatus newStatus, DateTime updatedAtUtc, string? errorCode = null, string? errorMessage = null)
  {
    if (Status == newStatus)
      return;

    var allowed = (Status, newStatus) switch
    {
      (PaymentStatus.Initiated, PaymentStatus.Authorized) => true,
      (PaymentStatus.Initiated, PaymentStatus.Captured) => true,
      (PaymentStatus.Initiated, PaymentStatus.Failed) => true,
      (PaymentStatus.Authorized, PaymentStatus.Captured) => true,
      (PaymentStatus.Authorized, PaymentStatus.Failed) => true,
      (PaymentStatus.Captured, PaymentStatus.Refunded) => true,
      _ => false
    };

    if (!allowed)
      throw new DomainException(ErrorCodes.OrderInvalidState, $"Transicion de estado de pago no permitida: {Status} → {newStatus}.");

    Status = newStatus;
    UpdatedAtUtc = updatedAtUtc;
    ErrorCode = errorCode;
    ErrorMessage = errorMessage;
  }
}
