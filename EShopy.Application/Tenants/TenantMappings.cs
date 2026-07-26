using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants;

/// <summary>Mapeos compartidos entre Tenant/Store (dominio) y sus DTOs de aplicación.</summary>
internal static class TenantMappings
{
  internal static TenantOnboardingResultDto ToOnboardingResultDto(Tenant tenant) => new()
  {
    TenantId = tenant.Id,
    Subdomain = tenant.Subdomain,
    Status = tenant.Status.ToString()
  };

  internal static TenantAdminDto ToAdminDto(Tenant tenant) => new()
  {
    Id = tenant.Id,
    Subdomain = tenant.Subdomain,
    BusinessName = tenant.BusinessName,
    Status = tenant.Status.ToString(),
    Plan = tenant.Plan.ToString(),
    CreatedAtUtc = tenant.CreatedAtUtc,
    ActivatedAtUtc = tenant.ActivatedAtUtc
  };

  internal static StoreProfileDto ToStoreProfileDto(Store store) => new()
  {
    StoreId = store.Id,
    Name = store.Name,
    CurrencyCode = store.CurrencyCode,
    Timezone = store.Timezone,
    PrimaryColor = store.PrimaryColor,
    LogoUrl = store.LogoUrl,
    BackgroundColor = store.BackgroundColor,
    Description = store.Description
  };
}
