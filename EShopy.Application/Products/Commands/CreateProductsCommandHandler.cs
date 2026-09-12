using EShopy.Application.Common.Context;
using EShopy.Application.Common.Stores;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Products;

namespace EShopy.Application.Products.Commands;

public sealed class CreateProductsCommandHandler(
  IProductRepository repository,
  IStoreService storeService,
  TenantContext tenantContext)
{
  private readonly CreateProductCommandValidator _validator = new();

  public async Task<Result<IReadOnlyList<ProductAdminDto>>> Handle(CreateProductsCommand command, CancellationToken ct)
  {
    // 0. Un lote vacío no tiene nada para crear
    if (command.Items.Count == 0)
      return Fail(ErrorCodes.ValidationError, "El lote no puede estar vacío.");

    // 1. Validación de entrada, item por item
    var validationErrors = new List<string>();
    for (var i = 0; i < command.Items.Count; i++)
    {
      var validation = _validator.Validate(command.Items[i]);
      if (!validation.IsValid)
        validationErrors.Add($"Item {i}: " + string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
    }
    if (validationErrors.Count > 0)
      return Fail(ErrorCodes.ValidationError, string.Join(" | ", validationErrors));

    // 2. Verificar tenant
    if (!tenantContext.TenantId.HasValue)
      return Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;

    // 3. Obtener Store (StoreId + CurrencyCode), compartido por todo el lote
    var store = await storeService.GetDefaultStoreAsync(tenantId, ct);
    if (store is null)
      return Fail(ErrorCodes.NotFound, "No existe un store configurado para este tenant.");

    // 4. Normalizar slug/SKU y verificar unicidad, tanto contra la base como dentro del propio lote
    var normalized = command.Items
      .Select(item => (Slug: item.Slug.Trim().ToLowerInvariant(), Sku: Product.NormalizeSku(item.Sku), Item: item))
      .ToList();

    var duplicateSlug = normalized.GroupBy(x => x.Slug).FirstOrDefault(g => g.Count() > 1);
    if (duplicateSlug is not null)
      return Fail(ErrorCodes.ValidationError, $"El slug '{duplicateSlug.Key}' aparece más de una vez en el lote.");

    var duplicateSku = normalized.Where(x => x.Sku is not null).GroupBy(x => x.Sku).FirstOrDefault(g => g.Count() > 1);
    if (duplicateSku is not null)
      return Fail(ErrorCodes.ValidationError, $"El SKU '{duplicateSku.Key}' aparece más de una vez en el lote.");

    foreach (var (slug, sku, _) in normalized)
    {
      if (await repository.SlugExistsAsync(tenantId, slug, null, ct))
        return Fail(ErrorCodes.Conflict, $"Ya existe un producto con el slug '{slug}'.");

      if (sku is not null && await repository.SkuExistsAsync(tenantId, sku, null, ct))
        return Fail(ErrorCodes.Conflict, $"Ya existe un producto con el SKU '{sku}'.");
    }

    // 5. Crear y persistir el lote completo en una sola transacción (todo o nada)
    try
    {
      var now = DateTime.UtcNow;
      var products = normalized
        .Select(x => Product.Create(
          tenantId, store.Id, x.Slug, x.Sku, x.Item.Name, x.Item.Description, x.Item.Price, x.Item.StockOnHand,
          store.CurrencyCode, now))
        .ToList();

      await repository.AddRangeAsync(products, ct);
      return Result<IReadOnlyList<ProductAdminDto>>.Ok(products.Select(ProductMappings.ToAdminDto).ToList());
    }
    catch (DomainException ex)
    {
      return Fail(ex.Code, ex.Message);
    }
  }

  private static Result<IReadOnlyList<ProductAdminDto>> Fail(string code, string message)
    => Result<IReadOnlyList<ProductAdminDto>>.Fail(code, message);
}
