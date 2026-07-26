namespace EShopy.Domain.Payments;

/// <summary>
/// Registro de idempotencia para webhooks de pago: si (Provider, EventId) ya existe aca, el evento
/// ya se proceso y el webhook debe responder 200 sin reaplicar nada. No es multi-tenant (el tenant
/// todavia no se conoce en el momento en que se hace esta verificacion, ver
/// ProcessPaymentWebhookCommandHandler) — el EventId ya es unico a nivel provider, no hace falta
/// acotarlo por tenant.
/// </summary>
public sealed class PaymentEventProcessed
{
  private PaymentEventProcessed(Guid id, string provider, string eventId, DateTime processedAtUtc)
  {
    Id = id;
    Provider = provider;
    EventId = eventId;
    ProcessedAtUtc = processedAtUtc;
  }

  public Guid Id { get; private set; }
  public string Provider { get; private set; }
  public string EventId { get; private set; }
  public DateTime ProcessedAtUtc { get; private set; }

  public static PaymentEventProcessed Create(string provider, string eventId, DateTime processedAtUtc)
    => new(Guid.NewGuid(), provider, eventId, processedAtUtc);
}
