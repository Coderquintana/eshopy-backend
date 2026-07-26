using EShopy.Application.Common.Audit;
using EShopy.Application.Common.Context;
using EShopy.Application.Common.Payments;
using EShopy.Application.Orders;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Orders;
using EShopy.Domain.Payments;

namespace EShopy.Application.Payments.Commands;

/// <summary>
/// Implementa el flujo de domain/payments.md "Flujo de webhook idempotente". El paso critico es el
/// orden: (1) validar firma, (2) chequear idempotencia, (3) recien ahi buscar el Payment SIN tenant
/// conocido (seguro por el Global Query Filter con TenantId == null, ver EShopyDbContext), (4) fijar
/// el tenant, (5) aplicar la transicion sobre Payment y Order, (6) persistir todo atomicamente.
/// </summary>
public sealed class ProcessPaymentWebhookCommandHandler(
  IEnumerable<IPaymentProviderAdapter> adapters,
  IPaymentWebhookWriter webhookWriter,
  IOrderRepository orderRepository,
  TenantContext tenantContext,
  IAuditLogger auditLogger)
{
  public async Task<Result> Handle(ProcessPaymentWebhookCommand command, CancellationToken ct)
  {
    var adapter = adapters.FirstOrDefault(a => a.Provider == command.Provider);
    if (adapter is null)
      return Result.Fail(ErrorCodes.NotFound, $"Provider de pago '{command.Provider}' no soportado.");

    if (!adapter.ValidateWebhookSignature(command.RawBody, command.Headers))
      return Result.Fail(ErrorCodes.PaymentWebhookInvalid, "Firma de webhook invalida.");

    var evt = adapter.ParseWebhook(command.RawBody);

    if (await webhookWriter.IsEventProcessedAsync(command.Provider, evt.EventId, ct))
      return Result.Ok(); // Idempotente: ya procesado, no reaplicar.

    var payment = await webhookWriter.FindByProviderPaymentIdAsync(command.Provider, evt.ProviderPaymentId, ct);
    if (payment is null)
      return Result.Fail(ErrorCodes.NotFound, "Payment no encontrado para este evento.");

    // Recien ahora se conoce el tenant — fijarlo antes de tocar Order (que si esta filtrado).
    tenantContext.Set(payment.TenantId);

    var order = await orderRepository.GetByIdAsync(payment.TenantId, payment.OrderId, ct);
    if (order is null)
      return Result.Fail(ErrorCodes.NotFound, "Order asociado al Payment no encontrado.");

    try
    {
      var now = DateTime.UtcNow;

      switch (evt.EventType)
      {
        case PaymentWebhookEventType.Captured:
          payment.ChangeStatus(PaymentStatus.Captured, now);
          if (order.Status == OrderStatus.PendingPayment)
            order.ChangeStatus(OrderStatus.Paid, now);
          break;

        case PaymentWebhookEventType.Failed:
          payment.ChangeStatus(PaymentStatus.Failed, now);
          if (order.Status == OrderStatus.PendingPayment)
            order.ChangeStatus(OrderStatus.Cancelled, now);
          break;

        case PaymentWebhookEventType.Refunded:
          payment.ChangeStatus(PaymentStatus.Refunded, now);
          if (order.Status == OrderStatus.Paid)
            order.ChangeStatus(OrderStatus.Refunded, now);
          break;
      }

      await webhookWriter.ApplyAsync(payment, order, command.Provider, evt.EventId, now, ct);
      await auditLogger.LogAsync(payment.TenantId, "Payment.Webhook", "Payment", payment.Id, $"{evt.EventType} ({command.Provider})", ct);
      return Result.Ok();
    }
    catch (DomainException ex)
    {
      return Result.Fail(ex.Code, ex.Message);
    }
  }
}
