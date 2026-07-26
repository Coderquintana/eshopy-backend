using EShopy.Application.Tenants.Commands;
using EShopy.Application.Tenants.Contracts;
using EShopy.Application.Tenants.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

/// <summary>Configuracion de la tienda del tenant resuelto por subdominio.</summary>
[Route("api/store")]
public sealed class StoreController(
  GetStoreQueryHandler getStoreHandler,
  UpdateStoreCommandHandler updateStoreHandler) : BaseApiController
{
  /// <summary>Configuracion publica de la tienda (branding, moneda, timezone).</summary>
  [HttpGet]
  [AllowAnonymous]
  [ProducesResponseType(typeof(StoreProfileDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<StoreProfileDto>> GetStore(CancellationToken ct)
  {
    var result = await getStoreHandler.Handle(new GetStoreQuery(), ct);
    return FromResult(result);
  }

  /// <summary>Actualiza el perfil de la tienda (nombre, timezone, branding).</summary>
  [HttpPut]
  [Authorize(Policy = "StoreWrite")]
  [ProducesResponseType(typeof(StoreProfileDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<StoreProfileDto>> UpdateStore([FromBody] UpdateStoreCommand command, CancellationToken ct)
  {
    var result = await updateStoreHandler.Handle(command, ct);
    return FromResult(result);
  }
}
