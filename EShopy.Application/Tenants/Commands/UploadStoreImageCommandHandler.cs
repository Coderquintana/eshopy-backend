using EShopy.Application.Common.Context;
using EShopy.Application.Tenants;
using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Tenants.Commands;

public sealed class UploadStoreImageCommandHandler(
  IStoreRepository repository,
  IStoreImageStorage imageStorage,
  TenantContext tenantContext)
{
  private readonly UploadStoreImageCommandValidator _validator = new();

  public async Task<Result<StoreProfileDto>> Handle(UploadStoreImageCommand command, CancellationToken ct)
  {
    var validation = _validator.Validate(command);
    if (!validation.IsValid)
    {
      var message = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage));
      return Result<StoreProfileDto>.Fail(ErrorCodes.ValidationError, message);
    }

    if (!tenantContext.TenantId.HasValue)
      return Result<StoreProfileDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var store = await repository.GetByTenantIdAsync(tenantContext.TenantId.Value, ct);
    if (store is null)
      return Result<StoreProfileDto>.Fail(ErrorCodes.NotFound, "Store no encontrado.");

    string imageUrl;
    try
    {
      imageUrl = await imageStorage.SaveAsync(
        store.Id,
        command.Kind,
        command.Content,
        command.FileName,
        command.ContentType,
        ct);
    }
    catch (InvalidDataException ex)
    {
      return Result<StoreProfileDto>.Fail(ErrorCodes.ValidationError, ex.Message);
    }

    var previousImageUrl = GetImageUrl(store, command.Kind);
    try
    {
      SetImageUrl(store, command.Kind, imageUrl);
      await repository.UpdateAsync(store, ct);
    }
    catch
    {
      await imageStorage.DeleteAsync(store.Id, imageUrl, CancellationToken.None);
      throw;
    }

    if (previousImageUrl is not null)
      await imageStorage.DeleteAsync(store.Id, previousImageUrl, CancellationToken.None);

    return Result<StoreProfileDto>.Ok(TenantMappings.ToStoreProfileDto(store));
  }

  internal static string? GetImageUrl(EShopy.Domain.Tenants.Store store, StoreImageKind kind)
    => kind == StoreImageKind.Logo ? store.LogoUrl : store.Theme?.HeroImageUrl;

  internal static void SetImageUrl(EShopy.Domain.Tenants.Store store, StoreImageKind kind, string? imageUrl)
  {
    if (kind == StoreImageKind.Logo)
      store.SetLogoUrl(imageUrl, DateTime.UtcNow);
    else
      store.SetHeroImageUrl(imageUrl, DateTime.UtcNow);
  }
}
