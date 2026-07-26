using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Tenants.Queries;

public sealed class GetTenantByIdQueryHandler(ITenantRepository repository)
{
  public async Task<Result<TenantAdminDto>> Handle(GetTenantByIdQuery query, CancellationToken ct)
  {
    var tenant = await repository.GetByIdAsync(query.Id, ct);
    if (tenant is null)
      return Result<TenantAdminDto>.Fail(ErrorCodes.NotFound, "Tenant no encontrado.");

    return Result<TenantAdminDto>.Ok(TenantMappings.ToAdminDto(tenant));
  }
}
