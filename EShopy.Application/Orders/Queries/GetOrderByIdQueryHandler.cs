using EShopy.Application.Common.Context;
using EShopy.Application.Orders.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Orders.Queries;

public sealed class GetOrderByIdQueryHandler(
  IOrderRepository repository,
  TenantContext tenantContext)
{
  public async Task<Result<OrderAdminDto>> Handle(GetOrderByIdQuery query, CancellationToken ct)
  {
    if (!tenantContext.TenantId.HasValue)
      return Result<OrderAdminDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var order = await repository.GetByIdAsync(tenantContext.TenantId.Value, query.Id, ct);
    if (order is null)
      return Result<OrderAdminDto>.Fail(ErrorCodes.NotFound, "Pedido no encontrado.");

    return Result<OrderAdminDto>.Ok(OrderMappings.ToAdminDto(order));
  }
}
