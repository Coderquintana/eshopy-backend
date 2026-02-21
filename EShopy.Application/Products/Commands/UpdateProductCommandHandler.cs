using EShopy.Application.Common.Context;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Products;

namespace EShopy.Application.Products.Commands;

public sealed class UpdateProductCommandHandler(
  IProductRepository repository,
  TenantContext tenantContext)
{
  private readonly UpdateProductCommandValidator _validator = new();

  public async Task<Result<ProductAdminDto>> Handle(UpdateProductCommand command, CancellationToken ct)
  {
    // 1. Validación de entrada
    var validation = _validator.Validate(command);
    if (!validation.IsValid)
    {
      var msg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      return Result<ProductAdminDto>.Fail(ErrorCodes.ValidationError, msg);
    }

    // 2. Verificar tenant
    if (!tenantContext.TenantId.HasValue)
      return Result<ProductAdminDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;

    // 3. Buscar producto
    var product = await repository.GetByIdAsync(tenantId, command.Id, ct);
    if (product is null)
      return Result<ProductAdminDto>.Fail(ErrorCodes.NotFound, "Producto no encontrado.");

    // 4. Unicidad de SKU (excluyendo el producto actual)
    var normalizedSku = Product.NormalizeSku(command.Sku);
    if (normalizedSku is not null && await repository.SkuExistsAsync(tenantId, normalizedSku, command.Id, ct))
      return Result<ProductAdminDto>.Fail(ErrorCodes.Conflict, "Ya existe un producto con ese SKU.");

    // 5. Aplicar cambios
    try
    {
      product.UpdateDetails(command.Name, command.Description, command.Price, command.StockOnHand, normalizedSku, DateTime.UtcNow);
      await repository.UpdateAsync(product, ct);
      return Result<ProductAdminDto>.Ok(ProductMappings.ToAdminDto(product));
    }
    catch (DomainException ex)
    {
      return Result<ProductAdminDto>.Fail(ex.Code, ex.Message);
    }
  }
}
