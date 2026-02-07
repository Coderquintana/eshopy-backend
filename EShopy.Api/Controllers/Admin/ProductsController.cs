using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Products;
using EShopy.Application.Products.Contracts;
using EShopy.Application.Products.Requests;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Admin;

/// <summary>Endpoints administrativos para el catálogo de productos.</summary>
[Route("api/products")]
public sealed class ProductsController(IProductService service) : BaseController
{
  /// <summary>Crea un producto en estado Draft.</summary>
  [HttpPost]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status201Created)]
  public async Task<ActionResult<ProductAdminDto>> Create([FromBody] CreateProductRequest request, CancellationToken ct)
  {
    var product = await service.CreateAsync(request, ct);
    return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
  }

  /// <summary>Lista productos (admin) con paginación.</summary>
  [HttpGet]
  [ProducesResponseType(typeof(PagedResult<ProductAdminDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<PagedResult<ProductAdminDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
  {
    var result = await service.GetAdminAsync(page, pageSize, ct);
    return Ok(result);
  }

  /// <summary>Obtiene el detalle de un producto por id.</summary>
  [HttpGet("{id:guid}")]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
  public async Task<ActionResult<ProductAdminDto>> GetById(Guid id, CancellationToken ct)
  {
    var product = await service.GetByIdAsync(id, ct);
    return Ok(product);
  }

  /// <summary>Actualiza campos editables de un producto.</summary>
  [HttpPut("{id:guid}")]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
  public async Task<ActionResult<ProductAdminDto>> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
  {
    var product = await service.UpdateAsync(id, request, ct);
    return Ok(product);
  }

  /// <summary>Cambia el estado del producto (Draft/Active/Archived).</summary>
  [HttpPatch("{id:guid}/status")]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
  public async Task<ActionResult<ProductAdminDto>> ChangeStatus(Guid id, [FromBody] ChangeProductStatusRequest request, CancellationToken ct)
  {
    var product = await service.ChangeStatusAsync(id, request, ct);
    return Ok(product);
  }
}
