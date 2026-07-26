using EShopy.Application.Common.Context;
using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Tenants.Commands;

public sealed class UpdateStoreCommandHandler(
  IStoreRepository repository,
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
      store.UpdateProfile(command.Name, command.Timezone, command.PrimaryColor, command.LogoUrl,
        command.BackgroundColor, command.Description, DateTime.UtcNow);

      await repository.UpdateAsync(store, ct);
      return Result<StoreProfileDto>.Ok(TenantMappings.ToStoreProfileDto(store));
    }
    catch (DomainException ex)
    {
      return Result<StoreProfileDto>.Fail(ex.Code, ex.Message);
    }
  }
}
