using EShopy.Application.Payments;
using EShopy.Domain.Orders;
using EShopy.Domain.Payments;
using EShopy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Payments;

public sealed class EfPaymentWebhookWriter(EShopyDbContext db) : IPaymentWebhookWriter
{
  public Task<Payment?> FindByProviderPaymentIdAsync(string provider, string providerPaymentId, CancellationToken ct)
    // Tracked (sin AsNoTracking) a proposito: el handler muta este mismo Payment y ApplyAsync lo
    // persiste en el mismo SaveChangesAsync, sin necesitar un Update() explicito.
    => db.Payments.FirstOrDefaultAsync(p => p.Provider == provider && p.ProviderPaymentId == providerPaymentId, ct);

  public Task<bool> IsEventProcessedAsync(string provider, string eventId, CancellationToken ct)
    => db.PaymentEventsProcessed.AnyAsync(e => e.Provider == provider && e.EventId == eventId, ct);

  public async Task ApplyAsync(Payment payment, Order order, string provider, string eventId, DateTime processedAtUtc, CancellationToken ct)
  {
    // Payment ya esta trackeado desde FindByProviderPaymentIdAsync; Order llego desde
    // IOrderRepository.GetByIdAsync (AsNoTracking) y hay que attachearlo explicitamente.
    db.Orders.Update(order);
    db.PaymentEventsProcessed.Add(PaymentEventProcessed.Create(provider, eventId, processedAtUtc));

    await db.SaveChangesAsync(ct);
  }
}
