using EShopy.Application.Common.Context;
using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Tenants.Queries;

public sealed class GetStoreQueryHandler(
  IStoreRepository repository,
  TenantContext tenantContext)
{
  public async Task<Result<StoreProfileDto>> Handle(GetStoreQuery query, CancellationToken ct)
  {
    if (!tenantContext.TenantId.HasValue)
      return Result<StoreProfileDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var store = await repository.GetByTenantIdAsync(tenantContext.TenantId.Value, ct);
    if (store is null)
      return Result<StoreProfileDto>.Fail(ErrorCodes.NotFound, "Store no encontrado.");

    return Result<StoreProfileDto>.Ok(TenantMappings.ToStoreProfileDto(store));
  }
}
