using EShopy.Domain.Orders;
using EShopy.Domain.Payments;

namespace EShopy.Application.Payments;

/// <summary>
/// Soporte de datos para el webhook de pagos: busqueda de Payment sin tenant conocido, chequeo de
/// idempotencia, y la escritura atomica final (Payment + Order + registro de idempotencia). Mismo
/// espiritu que ICheckoutWriter — un writer angosto de un solo caso de uso, no un IUnitOfWork
/// generico (ver GOVERNANCE.md).
/// </summary>
public interface IPaymentWebhookWriter
{
  /// <summary>
  /// Busca sin filtrar por tenant (TenantContext.TenantId todavia no esta fijado en este punto del
  /// flujo — es justamente lo que este metodo permite resolver).
  /// </summary>
  Task<Payment?> FindByProviderPaymentIdAsync(string provider, string providerPaymentId, CancellationToken ct);

  Task<bool> IsEventProcessedAsync(string provider, string eventId, CancellationToken ct);

  /// <summary>Persiste Payment + Order actualizados y el registro de idempotencia en un solo SaveChangesAsync.</summary>
  Task ApplyAsync(Payment payment, Order order, string provider, string eventId, DateTime processedAtUtc, CancellationToken ct);
}
