using EShopy.Application.Common.Context;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Products.Queries;

public sealed class GetProductByIdQueryHandler(
  IProductRepository repository,
  TenantContext tenantContext)
{
  public async Task<Result<ProductAdminDto>> Handle(GetProductByIdQuery query, CancellationToken ct)
  {
    if (!tenantContext.TenantId.HasValue)
      return Result<ProductAdminDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var product = await repository.GetByIdAsync(tenantContext.TenantId.Value, query.Id, ct);
    if (product is null)
      return Result<ProductAdminDto>.Fail(ErrorCodes.NotFound, "Producto no encontrado.");

    return Result<ProductAdminDto>.Ok(ProductMappings.ToAdminDto(product));
  }
}
