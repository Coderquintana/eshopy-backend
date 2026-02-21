using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Contracts;
using EShopy.Application.Products.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Admin;

/// <summary>Endpoints administrativos para el catálogo de productos.</summary>
[Route("api/products")]
public sealed class ProductsController(
  CreateProductCommandHandler createHandler,
  UpdateProductCommandHandler updateHandler,
  ChangeProductStatusCommandHandler changeStatusHandler,
  GetProductsQueryHandler getProductsHandler,
  GetProductByIdQueryHandler getByIdHandler) : BaseApiController
{
  /// <summary>Crea un producto en estado Draft.</summary>
  [HttpPost]
  [Authorize(Policy = "CatalogWrite")]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
  public async Task<ActionResult<ProductAdminDto>> Create([FromBody] CreateProductCommand command, CancellationToken ct)
  {
    var result = await createHandler.Handle(command, ct);
    if (!result.IsSuccess)
      return FromResult(result);

    return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
  }

  /// <summary>Lista productos (admin) con paginación SQL.</summary>
  [HttpGet]
  [Authorize(Policy = "CatalogRead")]
  [ProducesResponseType(typeof(PagedResult<ProductAdminDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<PagedResult<ProductAdminDto>>> Get(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
  {
    var result = await getProductsHandler.Handle(new GetProductsQuery(new PagedQuery(page, pageSize)), ct);
    return FromResult(result);
  }

  /// <summary>Obtiene el detalle de un producto por ID.</summary>
  [HttpGet("{id:guid}")]
  [Authorize(Policy = "CatalogRead")]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<ProductAdminDto>> GetById(Guid id, CancellationToken ct)
  {
    var result = await getByIdHandler.Handle(new GetProductByIdQuery(id), ct);
    return FromResult(result);
  }

  /// <summary>Actualiza campos editables de un producto.</summary>
  [HttpPut("{id:guid}")]
  [Authorize(Policy = "CatalogWrite")]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
  public async Task<ActionResult<ProductAdminDto>> Update(Guid id, [FromBody] UpdateProductCommand command, CancellationToken ct)
  {
    // El ID del producto viene en la ruta; se inyecta en el command recibido del body
    var result = await updateHandler.Handle(command with { Id = id }, ct);
    return FromResult(result);
  }

  /// <summary>Cambia el estado del producto (Draft/Active/Archived).</summary>
  [HttpPatch("{id:guid}/status")]
  [Authorize(Policy = "CatalogWrite")]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
  public async Task<ActionResult<ProductAdminDto>> ChangeStatus(Guid id, [FromBody] ChangeProductStatusCommand command, CancellationToken ct)
  {
    var result = await changeStatusHandler.Handle(command with { Id = id }, ct);
    return FromResult(result);
  }
}
