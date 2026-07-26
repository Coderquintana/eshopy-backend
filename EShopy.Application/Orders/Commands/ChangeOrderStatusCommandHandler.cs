using EShopy.Application.Common.Context;
using EShopy.Application.Orders.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Orders.Commands;

public sealed class ChangeOrderStatusCommandHandler(
  IOrderRepository repository,
  TenantContext tenantContext)
{
  public async Task<Result<OrderAdminDto>> Handle(ChangeOrderStatusCommand command, CancellationToken ct)
  {
    if (!tenantContext.TenantId.HasValue)
      return Result<OrderAdminDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var order = await repository.GetByIdAsync(tenantContext.TenantId.Value, command.Id, ct);
    if (order is null)
      return Result<OrderAdminDto>.Fail(ErrorCodes.NotFound, "Pedido no encontrado.");

    try
    {
      order.ChangeStatus(command.Status, DateTime.UtcNow);
      await repository.UpdateAsync(order, ct);
      return Result<OrderAdminDto>.Ok(OrderMappings.ToAdminDto(order));
    }
    catch (DomainException ex)
    {
      return Result<OrderAdminDto>.Fail(ex.Code, ex.Message);
    }
  }
}
