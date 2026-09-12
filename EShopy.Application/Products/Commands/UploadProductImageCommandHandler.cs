using EShopy.Application.Common.Context;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Products.Commands;

public sealed class UploadProductImageCommandHandler(
  IProductRepository repository,
  IProductImageStorage imageStorage,
  TenantContext tenantContext)
{
  private readonly UploadProductImageCommandValidator _validator = new();

  public async Task<Result<ProductAdminDto>> Handle(UploadProductImageCommand command, CancellationToken ct)
  {
    var validation = _validator.Validate(command);
    if (!validation.IsValid)
    {
      var message = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage));
      return Result<ProductAdminDto>.Fail(ErrorCodes.ValidationError, message);
    }

    if (!tenantContext.TenantId.HasValue)
      return Result<ProductAdminDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;
    var expectedRowVersion = ProductConcurrency.Decode(command.RowVersion);
    var product = await repository.GetByIdAsync(tenantId, command.Id, ct);
    if (product is null)
      return Result<ProductAdminDto>.Fail(ErrorCodes.NotFound, "Producto no encontrado.");

    if (!ProductConcurrency.Matches(product, expectedRowVersion))
      return Result<ProductAdminDto>.Fail(
        ErrorCodes.ConcurrencyConflict,
        "El producto fue modificado por otro usuario. Recargá los datos e intentá nuevamente.");

    string imageUrl;
    try
    {
      imageUrl = await imageStorage.SaveAsync(
        command.Content,
        command.FileName,
        command.ContentType,
        ct);
    }
    catch (InvalidDataException ex)
    {
      return Result<ProductAdminDto>.Fail(ErrorCodes.ValidationError, ex.Message);
    }

    var previousImageUrl = product.ImageUrl;
    try
    {
      product.SetImageUrl(imageUrl, DateTime.UtcNow);
      await repository.UpdateAsync(product, expectedRowVersion, ct);
    }
    catch (DomainException ex)
    {
      await imageStorage.DeleteAsync(imageUrl, CancellationToken.None);
      return Result<ProductAdminDto>.Fail(ex.Code, ex.Message);
    }
    catch
    {
      await imageStorage.DeleteAsync(imageUrl, CancellationToken.None);
      throw;
    }

    if (previousImageUrl is not null)
      await imageStorage.DeleteAsync(previousImageUrl, CancellationToken.None);

    return Result<ProductAdminDto>.Ok(ProductMappings.ToAdminDto(product));
  }
}
