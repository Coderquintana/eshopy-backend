using EShopy.Application.Payments;
using EShopy.Domain.Orders;
using EShopy.Domain.Payments;

namespace EShopy.Tests.Integration.Support;

/// <summary>
/// Payment/Order llegan mutados por referencia (ver InMemoryCheckoutWriter) — este writer solo
/// necesita registrar el evento como procesado, no "guardar" nada mas.
/// </summary>
internal sealed class InMemoryPaymentWebhookWriter(InMemoryOrdersState state) : IPaymentWebhookWriter
{
  public Task<Payment?> FindByProviderPaymentIdAsync(string provider, string providerPaymentId, CancellationToken ct)
  {
    lock (state.Sync)
    {
      var payment = state.PaymentsById.Values
        .FirstOrDefault(p => p.Provider == provider && p.ProviderPaymentId == providerPaymentId);

      return Task.FromResult(payment);
    }
  }

  public Task<bool> IsEventProcessedAsync(string provider, string eventId, CancellationToken ct)
  {
    lock (state.Sync)
    {
      return Task.FromResult(state.ProcessedEvents.Contains((provider, eventId)));
    }
  }

  public Task ApplyAsync(Payment payment, Order order, string provider, string eventId, DateTime processedAtUtc, CancellationToken ct)
  {
    lock (state.Sync)
    {
      state.ProcessedEvents.Add((provider, eventId));
    }

    return Task.CompletedTask;
  }
}
