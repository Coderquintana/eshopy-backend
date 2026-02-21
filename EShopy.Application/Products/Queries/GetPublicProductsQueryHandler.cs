using EShopy.Application.Common.Context;
using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Products.Queries;

public sealed class GetPublicProductsQueryHandler(
  IProductRepository repository,
  TenantContext tenantContext)
{
  public async Task<Result<PagedResult<ProductPublicDto>>> Handle(GetPublicProductsQuery query, CancellationToken ct)
  {
    if (!tenantContext.TenantId.HasValue)
      return Result<PagedResult<ProductPublicDto>>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;

    var page = query.Paging.Page < 1 ? 1 : query.Paging.Page;
    var pageSize = query.Paging.PageSize switch
    {
      < 1 => 1,
      > 100 => 100,
      _ => query.Paging.PageSize
    };

    var (items, totalCount) = await repository.GetPublicPagedAsync(tenantId, new PagedQuery(page, pageSize), ct);
    var dtos = items.Select(ProductMappings.ToPublicDto).ToList();

    return Result<PagedResult<ProductPublicDto>>.Ok(new PagedResult<ProductPublicDto>(dtos, page, pageSize, totalCount));
  }
}
