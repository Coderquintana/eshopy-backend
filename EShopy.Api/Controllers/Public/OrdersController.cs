using EShopy.Application.Orders.Contracts;
using EShopy.Application.Orders.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

/// <summary>
/// Consulta publica de pedidos para el comprador anonimo. Autoriza por posesion del AccessToken
/// devuelto en el checkout (header X-Order-Token), no por sesion: tras pagar, el provider redirige
/// al storefront en una navegacion nueva donde el resultado del checkout ya no esta en memoria.
/// </summary>
[AllowAnonymous]
[Route("api/public/orders")]
public sealed class OrdersController(GetPublicOrderQueryHandler getPublicOrderHandler) : BaseApiController
{
  private const string OrderTokenHeader = "X-Order-Token";

  /// <summary>Obtiene la vista reducida del pedido (sin datos personales del comprador).</summary>
  [HttpGet("{id:guid}")]
  [ProducesResponseType(typeof(PublicOrderDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<PublicOrderDto>> GetById(Guid id, CancellationToken ct)
  {
    var result = await getPublicOrderHandler.Handle(new GetPublicOrderQuery(id), GetOrderToken(), ct);
    return FromResult(result);
  }

  private string GetOrderToken()
    => Request.Headers.TryGetValue(OrderTokenHeader, out var value) ? value.ToString() : "";
}
