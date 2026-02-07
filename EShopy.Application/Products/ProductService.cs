using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Common.Context;
using EShopy.Application.Products.Contracts;
using EShopy.Application.Products.Requests;
using EShopy.Application.Products.Validation;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Products;

namespace EShopy.Application.Products;

public sealed class ProductService(TenantContext tenantContext, IProductRepository repository) : IProductService
{
  public async Task<ProductAdminDto> CreateAsync(CreateProductRequest request, CancellationToken ct)
  {
    ValidateRequest(new CreateProductRequestValidator(), request);

    var tenantId = EnsureTenantId();
    var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
    var normalizedSku = Product.NormalizeSku(request.Sku);

    if (await repository.SlugExistsAsync(tenantId, normalizedSlug, null, ct))
      throw new DomainException(ErrorCodes.Conflict, "Ya existe un producto con ese slug.");

    if (normalizedSku is not null && await repository.SkuExistsAsync(tenantId, normalizedSku, null, ct))
      throw new DomainException(ErrorCodes.Conflict, "Ya existe un producto con ese SKU.");

    var now = DateTime.UtcNow;
    var product = Product.Create(tenantId,
      normalizedSlug,
      normalizedSku,
      request.Name,
      request.Description,
      request.Price,
      request.StockOnHand,
      "PYG",
      now);

    await repository.AddAsync(product, ct);

    return ToAdminDto(product);
  }

  public async Task<PagedResult<ProductAdminDto>> GetAdminAsync(int page, int pageSize, CancellationToken ct)
  {
    var tenantId = EnsureTenantId();
    var items = await repository.GetAdminListAsync(tenantId, ct);

    var (paged, total, normalizedPage, normalizedPageSize) = Pagination.Apply(items, page, pageSize);
    var dtos = paged.Select(ToAdminDto).ToList();

    return new PagedResult<ProductAdminDto>(dtos, normalizedPage, normalizedPageSize, total);
  }

  public async Task<ProductAdminDto> GetByIdAsync(Guid id, CancellationToken ct)
  {
    var tenantId = EnsureTenantId();
    var product = await repository.GetByIdAsync(tenantId, id, ct);

    if (product is null)
      throw new DomainException(ErrorCodes.NotFound, "Producto no encontrado.");

    return ToAdminDto(product);
  }

  public async Task<ProductAdminDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct)
  {
    ValidateRequest(new UpdateProductRequestValidator(), request);

    var tenantId = EnsureTenantId();
    var product = await repository.GetByIdAsync(tenantId, id, ct);

    if (product is null)
      throw new DomainException(ErrorCodes.NotFound, "Producto no encontrado.");

    var normalizedSku = Product.NormalizeSku(request.Sku);
    if (normalizedSku is not null && await repository.SkuExistsAsync(tenantId, normalizedSku, id, ct))
      throw new DomainException(ErrorCodes.Conflict, "Ya existe un producto con ese SKU.");

    product.UpdateDetails(request.Name, request.Description, request.Price, request.StockOnHand, normalizedSku, DateTime.UtcNow);
    await repository.UpdateAsync(product, ct);

    return ToAdminDto(product);
  }

  public async Task<ProductAdminDto> ChangeStatusAsync(Guid id, ChangeProductStatusRequest request, CancellationToken ct)
  {
    ValidateRequest(new ChangeProductStatusRequestValidator(), request);

    var tenantId = EnsureTenantId();
    var product = await repository.GetByIdAsync(tenantId, id, ct);

    if (product is null)
      throw new DomainException(ErrorCodes.NotFound, "Producto no encontrado.");

    product.ChangeStatus(request.Status, DateTime.UtcNow);
    await repository.UpdateAsync(product, ct);

    return ToAdminDto(product);
  }

  public async Task<PagedResult<ProductPublicDto>> GetPublicAsync(int page, int pageSize, CancellationToken ct)
  {
    var tenantId = EnsureTenantId();
    var items = await repository.GetPublicListAsync(tenantId, ct);

    var (paged, total, normalizedPage, normalizedPageSize) = Pagination.Apply(items, page, pageSize);
    var dtos = paged.Select(ToPublicDto).ToList();

    return new PagedResult<ProductPublicDto>(dtos, normalizedPage, normalizedPageSize, total);
  }

  public async Task<ProductPublicDto> GetBySlugAsync(string slug, CancellationToken ct)
  {
    var tenantId = EnsureTenantId();
    var product = await repository.GetBySlugAsync(tenantId, slug.Trim(), ct);

    if (product is null || product.Status != ProductStatus.Active)
      throw new DomainException(ErrorCodes.NotFound, "Producto no encontrado.");

    return ToPublicDto(product);
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
      Sku = product.Sku,
      Name = product.Name,
      Description = product.Description,
      Price = product.Price,
      CurrencyCode = product.CurrencyCode,
      Status = product.Status.ToString(),
      StockOnHand = product.StockOnHand,
      CreatedAtUtc = product.CreatedAtUtc,
      UpdatedAtUtc = product.UpdatedAtUtc
    };

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

  private static void ValidateRequest<T>(FluentValidation.IValidator<T> validator, T request)
  {
    var result = validator.Validate(request);
    if (result.IsValid)
      return;

    var message = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
    throw new DomainException(ErrorCodes.ValidationError, message);
  }
}
