using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Common.Context;
using EShopy.Application.Products;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Products;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

[Route("api/public/products")]
public sealed class ProductsController(TenantContext tenantContext, IProductRepository repository) : BaseController
{
  [HttpGet]
  [ProducesResponseType(typeof(PagedResult<ProductPublicDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<PagedResult<ProductPublicDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
  {
    var tenantId = EnsureTenantId();
    var items = await repository.GetPublicListAsync(tenantId, ct);

    var (paged, total, normalizedPage, normalizedPageSize) = Paginate(items, page, pageSize);

    return Ok(new PagedResult<ProductPublicDto>(paged.Select(ToPublicDto).ToList(), normalizedPage, normalizedPageSize, total));
  }

  [HttpGet("{slug}")]
  [ProducesResponseType(typeof(ProductPublicDto), StatusCodes.Status200OK)]
  public async Task<ActionResult<ProductPublicDto>> GetBySlug(string slug, CancellationToken ct)
  {
    var tenantId = EnsureTenantId();
    var product = await repository.GetBySlugAsync(tenantId, slug.Trim(), ct);

    if (product is null || product.Status != ProductStatus.Active)
      throw new DomainException(ErrorCodes.NotFound, "Producto no encontrado.");

    return Ok(ToPublicDto(product));
  }

  private Guid EnsureTenantId()
  {
    if (!tenantContext.TenantId.HasValue)
      throw new DomainException(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    return tenantContext.TenantId.Value;
  }

  private static ProductPublicDto ToPublicDto(Product product)
    => new()
    {
      Id = product.Id,
      Slug = product.Slug,
      Name = product.Name,
      Description = product.Description,
      Price = product.Price,
      CurrencyCode = product.CurrencyCode
    };

  private static (IReadOnlyList<Product> Items, long Total, int Page, int PageSize) Paginate(IReadOnlyList<Product> items, int page, int pageSize)
  {
    var normalizedPage = page < 1 ? 1 : page;
    var normalizedPageSize = pageSize switch
    {
      < 1 => 1,
      > 100 => 100,
      _ => pageSize
    };

    var total = items.Count;
    var paged = items
      .Skip((normalizedPage - 1) * normalizedPageSize)
      .Take(normalizedPageSize)
      .ToList();

    return (paged, total, normalizedPage, normalizedPageSize);
  }
}
