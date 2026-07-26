using EShopy.Application.Common.Context;
using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Tenants.Queries;

public sealed class GetTenantUsersQueryHandler(
  ITenantUserRepository repository,
  TenantContext tenantContext)
{
  public async Task<Result<IReadOnlyList<TenantUserDto>>> Handle(GetTenantUsersQuery query, CancellationToken ct)
  {
    if (!tenantContext.TenantId.HasValue)
      return Result<IReadOnlyList<TenantUserDto>>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var users = await repository.GetByTenantIdAsync(tenantContext.TenantId.Value, ct);
    var dtos = users.Select(TenantMappings.ToUserDto).ToList();

    return Result<IReadOnlyList<TenantUserDto>>.Ok(dtos);
  }
}
