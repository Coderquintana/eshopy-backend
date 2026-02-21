using EShopy.Application.Common.Context;
using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Products.Queries;

public sealed class GetProductsQueryHandler(
  IProductRepository repository,
  TenantContext tenantContext)
{
  public async Task<Result<PagedResult<ProductAdminDto>>> Handle(GetProductsQuery query, CancellationToken ct)
  {
    if (!tenantContext.TenantId.HasValue)
      return Result<PagedResult<ProductAdminDto>>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;

    // Normalizar parámetros de paginación
    var page = query.Paging.Page < 1 ? 1 : query.Paging.Page;
    var pageSize = query.Paging.PageSize switch
    {
      < 1 => 1,
      > 100 => 100,
      _ => query.Paging.PageSize
    };

    var (items, totalCount) = await repository.GetAdminPagedAsync(tenantId, new PagedQuery(page, pageSize), ct);
    var dtos = items.Select(ProductMappings.ToAdminDto).ToList();

    return Result<PagedResult<ProductAdminDto>>.Ok(new PagedResult<ProductAdminDto>(dtos, page, pageSize, totalCount));
  }
}
