using EShopy.Application.Subscriptions;
using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants.Commands;

/// <summary>
/// Activacion manual (SUPERADMIN) de un tenant en PendingPayment. Herramienta de soporte/ops
/// permanente: cuando exista Payments (Fase 8), el webhook de pago la complementara, no la reemplaza.
/// </summary>
public sealed class ActivateTenantCommandHandler(
  ITenantRepository tenantRepository,
  ISubscriptionRepository subscriptionRepository,
  ITenantActivationWriter activationWriter)
{
  public async Task<Result<TenantAdminDto>> Handle(ActivateTenantCommand command, CancellationToken ct)
  {
    var tenant = await tenantRepository.GetByIdAsync(command.TenantId, ct);
    if (tenant is null)
      return Result<TenantAdminDto>.Fail(ErrorCodes.NotFound, "Tenant no encontrado.");

    var subscription = await subscriptionRepository.GetCurrentByTenantIdAsync(tenant.Id, ct);
    if (subscription is null)
      return Result<TenantAdminDto>.Fail(ErrorCodes.NotFound, "El tenant no tiene una suscripcion asociada.");

    try
    {
      var now = DateTime.UtcNow;
      tenant.ChangeStatus(TenantStatus.Active, now);
      subscription.ChangeStatus(SubscriptionStatus.Active, now);

      await activationWriter.ActivateAsync(tenant, subscription, ct);
      return Result<TenantAdminDto>.Ok(TenantMappings.ToAdminDto(tenant));
    }
    catch (DomainException ex)
    {
      return Result<TenantAdminDto>.Fail(ex.Code, ex.Message);
    }
  }
}
