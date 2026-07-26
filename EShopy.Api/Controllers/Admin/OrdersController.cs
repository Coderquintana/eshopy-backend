using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Orders.Commands;
using EShopy.Application.Orders.Contracts;
using EShopy.Application.Orders.Queries;
using EShopy.Application.Products.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Admin;

/// <summary>Endpoints administrativos para pedidos.</summary>
[Route("api/orders")]
public sealed class OrdersController(
  GetOrdersQueryHandler getOrdersHandler,
  GetOrderByIdQueryHandler getByIdHandler,
  ChangeOrderStatusCommandHandler changeStatusHandler) : BaseApiController
{
  /// <summary>Lista pedidos (admin) con paginación SQL.</summary>
  [HttpGet]
  [Authorize(Policy = "OrdersRead")]
  [ProducesResponseType(typeof(PagedResult<OrderAdminDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<PagedResult<OrderAdminDto>>> Get(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
  {
    var result = await getOrdersHandler.Handle(new GetOrdersQuery(new PagedQuery(page, pageSize)), ct);
    return FromResult(result);
  }

  /// <summary>Obtiene el detalle de un pedido por ID.</summary>
  [HttpGet("{id:guid}")]
  [Authorize(Policy = "OrdersRead")]
  [ProducesResponseType(typeof(OrderAdminDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<OrderAdminDto>> GetById(Guid id, CancellationToken ct)
  {
    var result = await getByIdHandler.Handle(new GetOrderByIdQuery(id), ct);
    return FromResult(result);
  }

  /// <summary>Cambia el estado del pedido (ej. marcar Cancelled/Refunded manualmente).</summary>
  [HttpPatch("{id:guid}/status")]
  [Authorize(Policy = "OrdersWrite")]
  [ProducesResponseType(typeof(OrderAdminDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
  public async Task<ActionResult<OrderAdminDto>> ChangeStatus(Guid id, [FromBody] ChangeOrderStatusCommand command, CancellationToken ct)
  {
    var result = await changeStatusHandler.Handle(command with { Id = id }, ct);
    return FromResult(result);
  }
}
