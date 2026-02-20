using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Products;
using EShopy.Application.Products.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

/// <summary>Endpoints públicos del catálogo.</summary>
[AllowAnonymous]
[Route("api/public/products")]
public sealed class ProductsController(IProductService service) : BaseApiController
{
  /// <summary>Lista productos públicos activos con paginación.</summary>
  [HttpGet]
  [ProducesResponseType(typeof(PagedResult<ProductPublicDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<PagedResult<ProductPublicDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
  {
    var result = await service.GetPublicAsync(page, pageSize, ct);
    return Ok(result);
  }

  /// <summary>Obtiene el detalle público por slug.</summary>
  [HttpGet("{slug}")]
  [ProducesResponseType(typeof(ProductPublicDto), StatusCodes.Status200OK)]
  public async Task<ActionResult<ProductPublicDto>> GetBySlug(string slug, CancellationToken ct)
  {
    var product = await service.GetBySlugAsync(slug, ct);
    return Ok(product);
  }
}
