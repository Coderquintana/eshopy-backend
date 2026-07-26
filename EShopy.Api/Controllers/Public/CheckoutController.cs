using EShopy.Application.Orders.Commands;
using EShopy.Application.Orders.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

/// <summary>Checkout publico: convierte el carrito identificado por X-Cart-Token en un Order.</summary>
[AllowAnonymous]
[Route("api/checkout")]
public sealed class CheckoutController(CheckoutCommandHandler checkoutHandler) : BaseApiController
{
  private const string CartTokenHeader = "X-Cart-Token";

  /// <summary>Crea el pedido a partir del carrito actual e inicia el pago.</summary>
  [HttpPost]
  [ProducesResponseType(typeof(CheckoutResultDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
  public async Task<ActionResult<CheckoutResultDto>> Checkout([FromBody] CheckoutCommand command, CancellationToken ct)
  {
    var result = await checkoutHandler.Handle(command, GetCartToken(), ct);
    return FromResult(result);
  }

  private string GetCartToken()
    => Request.Headers.TryGetValue(CartTokenHeader, out var value) ? value.ToString() : "";
}
