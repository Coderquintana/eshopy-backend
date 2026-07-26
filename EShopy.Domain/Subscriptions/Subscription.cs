using EShopy.Domain.Common.Entities;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Tenants;

namespace EShopy.Domain.Subscriptions;

/// <summary>
/// Suscripcion mensual del tenant a un plan. Solo una Active/PastDue por tenant a la vez
/// (invariante enforced por el repositorio, no por esta entidad).
/// </summary>
public sealed class Subscription : AppEntity
{
  private Subscription(Guid id,
    Guid tenantId,
    TenantPlan plan,
    SubscriptionStatus status,
    DateTime billingCycleStart,
    DateTime billingCycleEnd,
    decimal priceAmount,
    string currencyCode,
    string? externalSubscriptionId,
    DateTime? cancelledAtUtc,
    DateTime createdAtUtc,
    DateTime? updatedAtUtc)
    : base(id, tenantId, createdAtUtc, createdBy: null, updatedAtUtc, updatedBy: null, data: null)
  {
    Plan = plan;
    Status = status;
    BillingCycleStart = billingCycleStart;
    BillingCycleEnd = billingCycleEnd;
    PriceAmount = priceAmount;
    CurrencyCode = currencyCode;
    ExternalSubscriptionId = externalSubscriptionId;
    CancelledAtUtc = cancelledAtUtc;
  }

  public TenantPlan Plan { get; private set; }
  public SubscriptionStatus Status { get; private set; }

  /// <summary>
  /// Mientras Status es PendingActivation, BillingCycleStart == BillingCycleEnd (aun no hay ciclo real).
  /// Se recalculan a un ciclo de 1 mes cada vez que la suscripcion pasa a Active.
  /// </summary>
  public DateTime BillingCycleStart { get; private set; }
  public DateTime BillingCycleEnd { get; private set; }
  public decimal PriceAmount { get; private set; }
  public string CurrencyCode { get; private set; }
  public string? ExternalSubscriptionId { get; private set; }
  public DateTime? CancelledAtUtc { get; private set; }

  public static Subscription CreatePending(Guid tenantId, TenantPlan plan, decimal priceAmount, string currencyCode, DateTime createdAtUtc)
  {
    EnsurePrice(priceAmount);
    EnsureCurrency(currencyCode);

    return new Subscription(Guid.NewGuid(),
      tenantId,
      plan,
      SubscriptionStatus.PendingActivation,
      billingCycleStart: createdAtUtc,
      billingCycleEnd: createdAtUtc,
      priceAmount,
      currencyCode.Trim().ToUpperInvariant(),
      externalSubscriptionId: null,
      cancelledAtUtc: null,
      createdAtUtc,
      createdAtUtc);
  }

  /// <summary>Cambia el estado de la suscripcion validando las transiciones permitidas.</summary>
  /// <remarks>
  /// Transiciones validas:
  /// PendingActivation → Active | Active → PastDue | PastDue → Active
  /// PastDue → Suspended | Suspended → Active
  /// Active → Cancelled | Suspended → Cancelled
  /// </remarks>
  public void ChangeStatus(SubscriptionStatus newStatus, DateTime updatedAtUtc)
  {
    if (Status == newStatus)
      return;

    var allowed = (Status, newStatus) switch
    {
      (SubscriptionStatus.PendingActivation, SubscriptionStatus.Active) => true,
      (SubscriptionStatus.Active, SubscriptionStatus.PastDue) => true,
      (SubscriptionStatus.PastDue, SubscriptionStatus.Active) => true,
      (SubscriptionStatus.PastDue, SubscriptionStatus.Suspended) => true,
      (SubscriptionStatus.Suspended, SubscriptionStatus.Active) => true,
      (SubscriptionStatus.Active, SubscriptionStatus.Cancelled) => true,
      (SubscriptionStatus.Suspended, SubscriptionStatus.Cancelled) => true,
      _ => false
    };

    if (!allowed)
      throw new DomainException(
        ErrorCodes.TenantInvalidState,
        $"Transicion de estado de suscripcion no permitida: {Status} → {newStatus}.");

    Status = newStatus;
    UpdatedAtUtc = updatedAtUtc;

    if (newStatus == SubscriptionStatus.Active)
    {
      BillingCycleStart = updatedAtUtc;
      BillingCycleEnd = updatedAtUtc.AddMonths(1);
    }

    if (newStatus == SubscriptionStatus.Cancelled)
      CancelledAtUtc = updatedAtUtc;
  }

  private static void EnsurePrice(decimal priceAmount)
  {
    if (priceAmount < 0)
      throw new DomainException(ErrorCodes.ValidationError, "El precio de la suscripcion debe ser mayor o igual a cero.");
  }

  private static void EnsureCurrency(string currencyCode)
  {
    if (string.IsNullOrWhiteSpace(currencyCode))
      throw new DomainException(ErrorCodes.ValidationError, "El codigo de moneda es obligatorio.");
  }
}
