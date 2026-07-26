using EShopy.Application.Carts.Commands;
using EShopy.Application.Carts.Contracts;
using EShopy.Application.Carts.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

/// <summary>Carrito server-side, identificado por el header X-Cart-Token (UUID generado en el frontend).</summary>
[AllowAnonymous]
[Route("api/cart")]
public sealed class CartController(
  GetCartQueryHandler getCartHandler,
  AddCartItemCommandHandler addItemHandler,
  UpdateCartItemQuantityCommandHandler updateItemHandler,
  RemoveCartItemCommandHandler removeItemHandler) : BaseApiController
{
  private const string CartTokenHeader = "X-Cart-Token";

  /// <summary>Obtiene el carrito actual. Si el CartToken no tiene carrito todavia, retorna uno vacio.</summary>
  [HttpGet]
  [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
  public async Task<ActionResult<CartDto>> GetCart(CancellationToken ct)
  {
    var result = await getCartHandler.Handle(new GetCartQuery(), GetCartToken(), ct);
    return FromResult(result);
  }

  /// <summary>Agrega un producto al carrito. Si ya estaba, acumula la cantidad.</summary>
  [HttpPost("items")]
  [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
  public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemCommand command, CancellationToken ct)
  {
    var result = await addItemHandler.Handle(command, GetCartToken(), ct);
    return FromResult(result);
  }

  /// <summary>Actualiza la cantidad de un item existente.</summary>
  [HttpPut("items/{productId:guid}")]
  [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<CartDto>> UpdateItemQuantity(Guid productId, [FromBody] UpdateCartItemQuantityCommand command, CancellationToken ct)
  {
    var result = await updateItemHandler.Handle(command with { ProductId = productId }, GetCartToken(), ct);
    return FromResult(result);
  }

  /// <summary>Quita un item del carrito.</summary>
  [HttpDelete("items/{productId:guid}")]
  [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<CartDto>> RemoveItem(Guid productId, CancellationToken ct)
  {
    var result = await removeItemHandler.Handle(new RemoveCartItemCommand(productId), GetCartToken(), ct);
    return FromResult(result);
  }

  private string GetCartToken()
    => Request.Headers.TryGetValue(CartTokenHeader, out var value) ? value.ToString() : "";
}
