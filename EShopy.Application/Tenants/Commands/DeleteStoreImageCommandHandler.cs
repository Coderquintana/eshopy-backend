using EShopy.Application.Common.Context;
using EShopy.Application.Tenants;
using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Tenants.Commands;

public sealed class DeleteStoreImageCommandHandler(
  IStoreRepository repository,
  IStoreImageStorage imageStorage,
  TenantContext tenantContext)
{
  public async Task<Result<StoreProfileDto>> Handle(DeleteStoreImageCommand command, CancellationToken ct)
  {
    if (!Enum.IsDefined(command.Kind))
      return Result<StoreProfileDto>.Fail(ErrorCodes.ValidationError, "El tipo de imagen de la tienda no es válido.");

    if (!tenantContext.TenantId.HasValue)
      return Result<StoreProfileDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var store = await repository.GetByTenantIdAsync(tenantContext.TenantId.Value, ct);
    if (store is null)
      return Result<StoreProfileDto>.Fail(ErrorCodes.NotFound, "Store no encontrado.");

    var previousImageUrl = UploadStoreImageCommandHandler.GetImageUrl(store, command.Kind);
    UploadStoreImageCommandHandler.SetImageUrl(store, command.Kind, null);
    await repository.UpdateAsync(store, ct);

    if (previousImageUrl is not null)
      await imageStorage.DeleteAsync(store.Id, previousImageUrl, CancellationToken.None);

    return Result<StoreProfileDto>.Ok(TenantMappings.ToStoreProfileDto(store));
  }
}
