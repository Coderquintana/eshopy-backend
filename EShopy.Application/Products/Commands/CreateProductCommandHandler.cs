using EShopy.Application.Common.Context;
using EShopy.Application.Common.Stores;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Products;

namespace EShopy.Application.Products.Commands;

public sealed class CreateProductCommandHandler(
  IProductRepository repository,
  IStoreService storeService,
  TenantContext tenantContext)
{
  private readonly CreateProductCommandValidator _validator = new();

  public async Task<Result<ProductAdminDto>> Handle(CreateProductCommand command, CancellationToken ct)
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

    // 3. Obtener Store (StoreId + CurrencyCode)
    var store = await storeService.GetDefaultStoreAsync(tenantId, ct);
    if (store is null)
      return Result<ProductAdminDto>.Fail(ErrorCodes.NotFound, "No existe un store configurado para este tenant.");

    // 4. Unicidad de slug y SKU
    var normalizedSlug = command.Slug.Trim().ToLowerInvariant();
    var normalizedSku = Product.NormalizeSku(command.Sku);

    if (await repository.SlugExistsAsync(tenantId, normalizedSlug, null, ct))
      return Result<ProductAdminDto>.Fail(ErrorCodes.Conflict, "Ya existe un producto con ese slug.");

    if (normalizedSku is not null && await repository.SkuExistsAsync(tenantId, normalizedSku, null, ct))
      return Result<ProductAdminDto>.Fail(ErrorCodes.Conflict, "Ya existe un producto con ese SKU.");

    // 5. Crear y persistir
    try
    {
      var product = Product.Create(
        tenantId,
        store.Id,
        normalizedSlug,
        normalizedSku,
        command.Name,
        command.Description,
        command.Price,
        command.StockOnHand,
        store.CurrencyCode,
        DateTime.UtcNow);

      await repository.AddAsync(product, ct);
      return Result<ProductAdminDto>.Ok(ProductMappings.ToAdminDto(product));
    }
    catch (DomainException ex)
    {
      return Result<ProductAdminDto>.Fail(ex.Code, ex.Message);
    }
  }
}
