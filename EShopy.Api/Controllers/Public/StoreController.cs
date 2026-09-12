using EShopy.Application.Tenants;
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
  UpdateStoreCommandHandler updateStoreHandler,
  UploadStoreImageCommandHandler uploadImageHandler,
  DeleteStoreImageCommandHandler deleteImageHandler) : BaseApiController
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

  /// <summary>Sube o reemplaza el logo o la portada pública de la tienda.</summary>
  [HttpPost("images/{kind}")]
  [Authorize(Policy = "StoreWrite")]
  [Consumes("multipart/form-data")]
  [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
  [RequestSizeLimit(6 * 1024 * 1024)]
  [ProducesResponseType(typeof(StoreProfileDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<StoreProfileDto>> UploadImage(
    StoreImageKind kind,
    [FromForm] IFormFile? file,
    CancellationToken ct)
  {
    await using var content = file?.OpenReadStream() ?? Stream.Null;
    var command = new UploadStoreImageCommand(
      kind,
      content,
      file?.FileName ?? string.Empty,
      file?.ContentType ?? string.Empty,
      file?.Length ?? 0);
    var result = await uploadImageHandler.Handle(command, ct);
    return FromResult(result);
  }

  /// <summary>Quita el logo o la portada personalizada y vuelve al fallback de la plantilla.</summary>
  [HttpDelete("images/{kind}")]
  [Authorize(Policy = "StoreWrite")]
  [ProducesResponseType(typeof(StoreProfileDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<StoreProfileDto>> DeleteImage(StoreImageKind kind, CancellationToken ct)
  {
    var result = await deleteImageHandler.Handle(new DeleteStoreImageCommand(kind), ct);
    return FromResult(result);
  }
}
