using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Products.Contracts;
using EShopy.Application.Products.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

/// <summary>Endpoints públicos del catálogo.</summary>
[AllowAnonymous]
[Route("api/public/products")]
public sealed class ProductsController(
  GetPublicProductsQueryHandler getPublicHandler,
  GetProductBySlugQueryHandler getBySlugHandler) : BaseApiController
{
  /// <summary>Lista productos activos con paginación SQL.</summary>
  [HttpGet]
  [ProducesResponseType(typeof(PagedResult<ProductPublicDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<PagedResult<ProductPublicDto>>> Get(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
  {
    var result = await getPublicHandler.Handle(new GetPublicProductsQuery(new PagedQuery(page, pageSize)), ct);
    return FromResult(result);
  }

  /// <summary>Obtiene el detalle público de un producto por slug (solo productos Active).</summary>
  [HttpGet("{slug}")]
  [ProducesResponseType(typeof(ProductPublicDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<ProductPublicDto>> GetBySlug(string slug, CancellationToken ct)
  {
    var result = await getBySlugHandler.Handle(new GetProductBySlugQuery(slug), ct);
    return FromResult(result);
  }
}
