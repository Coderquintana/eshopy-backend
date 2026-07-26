using EShopy.Application.Common.Identity;
using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants.Commands;

public sealed class CreateTenantCommandHandler(
  ITenantRepository tenantRepository,
  ITenantOnboardingWriter onboardingWriter,
  IKeycloakUserProvisioner keycloakProvisioner)
{
  private readonly CreateTenantCommandValidator _validator = new();

  public async Task<Result<TenantOnboardingResultDto>> Handle(CreateTenantCommand command, CancellationToken ct)
  {
    // 1. Validación de entrada
    var validation = _validator.Validate(command);
    if (!validation.IsValid)
    {
      var msg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      return Result<TenantOnboardingResultDto>.Fail(ErrorCodes.ValidationError, msg);
    }

    var normalizedSubdomain = command.Subdomain.Trim().ToLowerInvariant();
    var plan = ParsePlan(command.Plan);

    // 2. Unicidad de subdominio
    if (await tenantRepository.SubdomainExistsAsync(normalizedSubdomain, ct))
      return Result<TenantOnboardingResultDto>.Fail(ErrorCodes.Conflict, "Ya existe un tenant con ese subdominio.");

    // 3. Crear al Owner en Keycloak ANTES de escribir en la base local: si esto falla, no queda
    //    un Tenant huerfano sin usuario (evita necesitar una transaccion compensatoria).
    var keycloakUserId = await keycloakProvisioner.CreateOwnerUserAsync(
      command.OwnerEmail, command.OwnerName, normalizedSubdomain, ct);

    // 4. Crear y persistir Tenant + Store + TenantUser(Owner) + Subscription en una transaccion
    try
    {
      var now = DateTime.UtcNow;
      var tenant = Tenant.Create(normalizedSubdomain, command.BusinessName, plan, now);
      var store = Store.CreateDefault(tenant.Id, command.BusinessName, now);
      var owner = TenantUser.Create(tenant.Id, keycloakUserId, command.OwnerEmail, command.OwnerName, TenantUserRole.Owner, now);

      var (price, currencyCode) = PlanPricing.For(plan);
      var subscription = Subscription.CreatePending(tenant.Id, plan, price, currencyCode, now);

      await onboardingWriter.CreateAsync(tenant, store, owner, subscription, ct);
      return Result<TenantOnboardingResultDto>.Ok(TenantMappings.ToOnboardingResultDto(tenant));
    }
    catch (DomainException ex)
    {
      return Result<TenantOnboardingResultDto>.Fail(ex.Code, ex.Message);
    }
  }

  private static TenantPlan ParsePlan(string plan) => plan.Trim().ToLowerInvariant() switch
  {
    "basic" => TenantPlan.Basic,
    "gold" => TenantPlan.Gold,
    "diamond" => TenantPlan.Diamond,
    _ => throw new DomainException(ErrorCodes.ValidationError, $"Plan desconocido: {plan}.")
  };
}
