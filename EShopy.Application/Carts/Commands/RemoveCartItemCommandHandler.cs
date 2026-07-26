using EShopy.Application.Carts.Contracts;
using EShopy.Application.Common.Context;
using EShopy.Application.Common.Stores;
using EShopy.Application.Products;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Carts.Commands;

public sealed class RemoveCartItemCommandHandler(
  ICartRepository cartRepository,
  IProductRepository productRepository,
  IStoreService storeService,
  TenantContext tenantContext)
{
  public async Task<Result<CartDto>> Handle(RemoveCartItemCommand command, string cartToken, CancellationToken ct)
  {
    if (command.ProductId == Guid.Empty)
      return Result<CartDto>.Fail(ErrorCodes.ValidationError, "El producto es obligatorio.");

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
      cart.RemoveItem(command.ProductId, DateTime.UtcNow);
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
