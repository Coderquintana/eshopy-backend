using EShopy.Application.Common.Context;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Products;

namespace EShopy.Application.Products.Queries;

public sealed class GetProductBySlugQueryHandler(
  IProductRepository repository,
  TenantContext tenantContext)
{
  public async Task<Result<ProductPublicDto>> Handle(GetProductBySlugQuery query, CancellationToken ct)
  {
    if (!tenantContext.TenantId.HasValue)
      return Result<ProductPublicDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var product = await repository.GetBySlugAsync(tenantContext.TenantId.Value, query.Slug.Trim(), ct);

    // Solo se expone si está en estado Active
    if (product is null || product.Status != ProductStatus.Active)
      return Result<ProductPublicDto>.Fail(ErrorCodes.NotFound, "Producto no encontrado.");

    return Result<ProductPublicDto>.Ok(ProductMappings.ToPublicDto(product));
  }
}
