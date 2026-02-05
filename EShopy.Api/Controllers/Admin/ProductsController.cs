using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Common.Context;
using EShopy.Application.Products;
using EShopy.Application.Products.Contracts;
using EShopy.Application.Products.Requests;
using EShopy.Application.Products.Validation;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Products;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Admin;

[Route("api/products")]
public sealed class ProductsController(TenantContext tenantContext, IProductRepository repository) : BaseController
{
  [HttpPost]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status201Created)]
  public async Task<ActionResult<ProductAdminDto>> Create([FromBody] CreateProductRequest request, CancellationToken ct)
  {
    ValidateRequest(new CreateProductRequestValidator(), request);

    var tenantId = EnsureTenantId();
    var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

    if (await repository.SlugExistsAsync(tenantId, normalizedSlug, null, ct))
      throw new DomainException(ErrorCodes.Conflict, "Ya existe un producto con ese slug.");

    var now = DateTime.UtcNow;
    var product = Product.Create(tenantId,
      normalizedSlug,
      request.Name,
      request.Description,
      request.Price,
      request.StockOnHand,
      "PYG",
      now);

    await repository.AddAsync(product, ct);

    return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToAdminDto(product));
  }

  [HttpGet]
  [ProducesResponseType(typeof(PagedResult<ProductAdminDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<PagedResult<ProductAdminDto>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
  {
    var tenantId = EnsureTenantId();
    var items = await repository.GetAdminListAsync(tenantId, ct);

    var (paged, total, normalizedPage, normalizedPageSize) = Paginate(items, page, pageSize);

    return Ok(new PagedResult<ProductAdminDto>(paged.Select(ToAdminDto).ToList(), normalizedPage, normalizedPageSize, total));
  }

  [HttpGet("{id:guid}")]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
  public async Task<ActionResult<ProductAdminDto>> GetById(Guid id, CancellationToken ct)
  {
    var tenantId = EnsureTenantId();
    var product = await repository.GetByIdAsync(tenantId, id, ct);

    if (product is null)
      throw new DomainException(ErrorCodes.NotFound, "Producto no encontrado.");

    return Ok(ToAdminDto(product));
  }

  [HttpPut("{id:guid}")]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
  public async Task<ActionResult<ProductAdminDto>> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
  {
    ValidateRequest(new UpdateProductRequestValidator(), request);

    var tenantId = EnsureTenantId();
    var product = await repository.GetByIdAsync(tenantId, id, ct);

    if (product is null)
      throw new DomainException(ErrorCodes.NotFound, "Producto no encontrado.");

    product.UpdateDetails(request.Name, request.Description, request.Price, request.StockOnHand, DateTime.UtcNow);
    await repository.UpdateAsync(product, ct);

    return Ok(ToAdminDto(product));
  }

  [HttpPatch("{id:guid}/status")]
  [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
  public async Task<ActionResult<ProductAdminDto>> ChangeStatus(Guid id, [FromBody] ChangeProductStatusRequest request, CancellationToken ct)
  {
    ValidateRequest(new ChangeProductStatusRequestValidator(), request);

    var tenantId = EnsureTenantId();
    var product = await repository.GetByIdAsync(tenantId, id, ct);

    if (product is null)
      throw new DomainException(ErrorCodes.NotFound, "Producto no encontrado.");

    product.ChangeStatus(request.Status, DateTime.UtcNow);
    await repository.UpdateAsync(product, ct);

    return Ok(ToAdminDto(product));
  }

  private Guid EnsureTenantId()
  {
    if (!tenantContext.TenantId.HasValue)
      throw new DomainException(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    return tenantContext.TenantId.Value;
  }

  private static ProductAdminDto ToAdminDto(Product product)
    => new()
    {
      Id = product.Id,
      Slug = product.Slug,
      Name = product.Name,
      Description = product.Description,
      Price = product.Price,
      CurrencyCode = product.CurrencyCode,
      Status = product.Status.ToString(),
      StockOnHand = product.StockOnHand,
      CreatedAtUtc = product.CreatedAtUtc,
      UpdatedAtUtc = product.UpdatedAtUtc
    };

  private static void ValidateRequest<T>(FluentValidation.IValidator<T> validator, T request)
  {
    var result = validator.Validate(request);
    if (result.IsValid)
      return;

    var message = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
    throw new DomainException(ErrorCodes.ValidationError, message);
  }

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
