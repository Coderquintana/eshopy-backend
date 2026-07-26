using EShopy.Application.Common.Context;
using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Orders.Contracts;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Orders.Queries;

public sealed class GetOrdersQueryHandler(
  IOrderRepository repository,
  TenantContext tenantContext)
{
  public async Task<Result<PagedResult<OrderAdminDto>>> Handle(GetOrdersQuery query, CancellationToken ct)
  {
    if (!tenantContext.TenantId.HasValue)
      return Result<PagedResult<OrderAdminDto>>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;

    var page = query.Paging.Page < 1 ? 1 : query.Paging.Page;
    var pageSize = query.Paging.PageSize switch
    {
      < 1 => 1,
      > 100 => 100,
      _ => query.Paging.PageSize
    };

    var (items, totalCount) = await repository.GetPagedAsync(tenantId, new PagedQuery(page, pageSize), ct);
    var dtos = items.Select(OrderMappings.ToAdminDto).ToList();

    return Result<PagedResult<OrderAdminDto>>.Ok(new PagedResult<OrderAdminDto>(dtos, page, pageSize, totalCount));
  }
}
