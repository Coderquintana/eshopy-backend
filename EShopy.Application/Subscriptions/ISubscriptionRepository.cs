using EShopy.Domain.Subscriptions;

namespace EShopy.Application.Subscriptions;

public interface ISubscriptionRepository
{
  /// <summary>Retorna la unica suscripcion no terminal (PendingActivation/Active/PastDue/Suspended) del tenant, si existe.</summary>
  Task<Subscription?> GetCurrentByTenantIdAsync(Guid tenantId, CancellationToken ct);
}
