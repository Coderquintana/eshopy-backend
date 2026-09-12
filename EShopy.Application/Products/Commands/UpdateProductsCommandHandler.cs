using EShopy.Application.Common.Audit;
using EShopy.Application.Common.Context;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Products;

namespace EShopy.Application.Products.Commands;

public sealed class UpdateProductsCommandHandler(
  IProductRepository repository,
  TenantContext tenantContext,
  IAuditLogger auditLogger)
{
  private readonly UpdateProductCommandValidator _validator = new();

  public async Task<Result<IReadOnlyList<ProductAdminDto>>> Handle(UpdateProductsCommand command, CancellationToken ct)
  {
    // 0. Un lote vacío no tiene nada para actualizar
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

    var duplicateId = command.Items.GroupBy(x => x.Id).FirstOrDefault(g => g.Count() > 1);
    if (duplicateId is not null)
      return Fail(ErrorCodes.ValidationError, $"El producto '{duplicateId.Key}' aparece más de una vez en el lote.");

    // 2. Verificar tenant
    if (!tenantContext.TenantId.HasValue)
      return Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;

    // 3. Buscar cada producto y validar concurrencia ANTES de tocar nada: si uno solo esta
    //    desactualizado, se rechaza el lote completo, no solo ese item.
    var entries = new List<(Product Product, byte[] ExpectedRowVersion, UpdateProductCommand Item, decimal PreviousPrice)>();
    foreach (var item in command.Items)
    {
      var product = await repository.GetByIdAsync(tenantId, item.Id, ct);
      if (product is null)
        return Fail(ErrorCodes.NotFound, $"Producto '{item.Id}' no encontrado.");

      var expectedRowVersion = ProductConcurrency.Decode(item.RowVersion);
      if (!ProductConcurrency.Matches(product, expectedRowVersion))
        return Fail(
          ErrorCodes.ConcurrencyConflict,
          "El producto fue modificado por otro usuario. Recargá los datos e intentá nuevamente.");

      entries.Add((product, expectedRowVersion, item, product.Price));
    }

    // 4. Unicidad de SKU: contra la base (excluyendo el propio producto) y dentro del propio lote
    var normalizedSkus = entries
      .Select(e => (e.Item.Id, Sku: Product.NormalizeSku(e.Item.Sku)))
      .Where(x => x.Sku is not null)
      .ToList();

    var duplicateSku = normalizedSkus.GroupBy(x => x.Sku).FirstOrDefault(g => g.Count() > 1);
    if (duplicateSku is not null)
      return Fail(ErrorCodes.ValidationError, $"El SKU '{duplicateSku.Key}' aparece más de una vez en el lote.");

    foreach (var (id, sku) in normalizedSkus)
    {
      if (await repository.SkuExistsAsync(tenantId, sku!, id, ct))
        return Fail(ErrorCodes.Conflict, $"Ya existe un producto con el SKU '{sku}'.");
    }

    // 5. Aplicar cambios y persistir el lote completo en una sola transacción (todo o nada)
    try
    {
      var now = DateTime.UtcNow;
      foreach (var (product, _, item, _) in entries)
      {
        var normalizedSku = Product.NormalizeSku(item.Sku);
        product.UpdateDetails(item.Name, item.Description, item.Price, item.StockOnHand, normalizedSku, now);
      }

      await repository.UpdateRangeAsync(entries.Select(e => (e.Product, e.ExpectedRowVersion)).ToList(), ct);

      foreach (var (product, _, _, previousPrice) in entries)
      {
        if (previousPrice == product.Price) continue;
        var details = FormattableString.Invariant($"{previousPrice} -> {product.Price}");
        await auditLogger.LogAsync(tenantId, "Product.ChangePrice", "Product", product.Id, details, ct);
      }

      return Result<IReadOnlyList<ProductAdminDto>>.Ok(entries.Select(e => ProductMappings.ToAdminDto(e.Product)).ToList());
    }
    catch (DomainException ex)
    {
      return Fail(ex.Code, ex.Message);
    }
  }

  private static Result<IReadOnlyList<ProductAdminDto>> Fail(string code, string message)
    => Result<IReadOnlyList<ProductAdminDto>>.Fail(code, message);
}
