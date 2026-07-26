using EShopy.Application.Carts.Contracts;
using EShopy.Application.Common.Context;
using EShopy.Application.Common.Stores;
using EShopy.Application.Products;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Carts.Commands;

public sealed class UpdateCartItemQuantityCommandHandler(
  ICartRepository cartRepository,
  IProductRepository productRepository,
  IStoreService storeService,
  TenantContext tenantContext)
{
  private readonly UpdateCartItemQuantityCommandValidator _validator = new();

  public async Task<Result<CartDto>> Handle(UpdateCartItemQuantityCommand command, string cartToken, CancellationToken ct)
  {
    var validation = _validator.Validate(command);
    if (!validation.IsValid)
    {
      var msg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      return Result<CartDto>.Fail(ErrorCodes.ValidationError, msg);
    }

    if (!tenantContext.TenantId.HasValue)
      return Result<CartDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;

    var cart = await cartRepository.GetByCartTokenAsync(tenantId, cartToken, ct);
    if (cart is null)
      return Result<CartDto>.Fail(ErrorCodes.NotFound, "Carrito no encontrado.");

    var store = await storeService.GetDefaultStoreAsync(tenantId, ct);
    if (store is null)
      return Result<CartDto>.Fail(ErrorCodes.NotFound, "No existe un store configurado para este tenant.");

    try
    {
      cart.UpdateItemQuantity(command.ProductId, command.Quantity, DateTime.UtcNow);
      await cartRepository.SaveChangesAsync(ct);

      var dto = await CartMappings.ToDtoAsync(cart, store.CurrencyCode, productRepository, ct);
      return Result<CartDto>.Ok(dto);
    }
    catch (DomainException ex)
    {
      return Result<CartDto>.Fail(ex.Code, ex.Message);
    }
  }
}
