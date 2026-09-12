using EShopy.Application.Common.Context;
using EShopy.Application.Tenants;
using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants.Commands;

public sealed class UpdateStoreCommandHandler(
  IStoreRepository repository,
  IStoreImageStorage imageStorage,
  TenantContext tenantContext)
{
  private readonly UpdateStoreCommandValidator _validator = new();

  public async Task<Result<StoreProfileDto>> Handle(UpdateStoreCommand command, CancellationToken ct)
  {
    var validation = _validator.Validate(command);
    if (!validation.IsValid)
    {
      var msg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      return Result<StoreProfileDto>.Fail(ErrorCodes.ValidationError, msg);
    }

    if (!tenantContext.TenantId.HasValue)
      return Result<StoreProfileDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var store = await repository.GetByTenantIdAsync(tenantContext.TenantId.Value, ct);
    if (store is null)
      return Result<StoreProfileDto>.Fail(ErrorCodes.NotFound, "Store no encontrado.");

    try
    {
      var previousLogoUrl = store.LogoUrl;
      var previousHeroImageUrl = store.Theme?.HeroImageUrl;
      var updatedAtUtc = DateTime.UtcNow;
      store.UpdateProfile(command.Name, command.Timezone, command.PrimaryColor, command.LogoUrl,
        command.BackgroundColor, command.Description, updatedAtUtc);
      store.UpdateContactInformation(command.ContactWhatsapp, command.ContactEmail,
        command.InstagramUrl, command.FacebookUrl, command.Address, command.BusinessHours,
        updatedAtUtc);
      store.UpdateTheme(new StoreTheme(command.FontFamily, command.HeadingScale,
        command.BorderRadius, command.SpacingDensity, command.HeroImageUrl), updatedAtUtc);

      await repository.UpdateAsync(store, ct);

      if (previousLogoUrl is not null && previousLogoUrl != store.LogoUrl)
        await imageStorage.DeleteAsync(store.Id, previousLogoUrl, CancellationToken.None);
      if (previousHeroImageUrl is not null && previousHeroImageUrl != store.Theme?.HeroImageUrl)
        await imageStorage.DeleteAsync(store.Id, previousHeroImageUrl, CancellationToken.None);

      return Result<StoreProfileDto>.Ok(TenantMappings.ToStoreProfileDto(store));
    }
    catch (DomainException ex)
    {
      return Result<StoreProfileDto>.Fail(ex.Code, ex.Message);
    }
  }
}
