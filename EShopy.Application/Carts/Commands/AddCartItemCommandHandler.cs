using EShopy.Application.Common.Context;
using EShopy.Application.Common.Stores;
using EShopy.Application.Carts.Contracts;
using EShopy.Application.Products;
using EShopy.Domain.Carts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Products;

namespace EShopy.Application.Carts.Commands;

public sealed class AddCartItemCommandHandler(
  ICartRepository cartRepository,
  IProductRepository productRepository,
  IStoreService storeService,
  TenantContext tenantContext)
{
  private readonly AddCartItemCommandValidator _validator = new();

  public async Task<Result<CartDto>> Handle(AddCartItemCommand command, string cartToken, CancellationToken ct)
  {
    var validation = _validator.Validate(command);
    if (!validation.IsValid)
    {
      var msg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      return Result<CartDto>.Fail(ErrorCodes.ValidationError, msg);
    }

    if (string.IsNullOrWhiteSpace(cartToken))
      return Result<CartDto>.Fail(ErrorCodes.ValidationError, "El header X-Cart-Token es obligatorio.");

    if (!tenantContext.TenantId.HasValue)
      return Result<CartDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;

    var product = await productRepository.GetByIdAsync(tenantId, command.ProductId, ct);
    if (product is null || product.Status != ProductStatus.Active)
      return Result<CartDto>.Fail(ErrorCodes.ProductNotAvailable, "El producto no esta disponible.");

    var store = await storeService.GetDefaultStoreAsync(tenantId, ct);
    if (store is null)
      return Result<CartDto>.Fail(ErrorCodes.NotFound, "No existe un store configurado para este tenant.");

    try
    {
      var now = DateTime.UtcNow;
      var cart = await cartRepository.GetByCartTokenAsync(tenantId, cartToken, ct);

      if (cart is null)
      {
        cart = Cart.Create(tenantId, cartToken, now);
        cart.AddItem(command.ProductId, command.Quantity, now);
        await cartRepository.AddAsync(cart, ct);
      }
      else
      {
        cart.AddItem(command.ProductId, command.Quantity, now);
        await cartRepository.SaveChangesAsync(ct);
      }

      var dto = await CartMappings.ToDtoAsync(cart, store.CurrencyCode, productRepository, ct);
      return Result<CartDto>.Ok(dto);
    }
    catch (DomainException ex)
    {
      return Result<CartDto>.Fail(ex.Code, ex.Message);
    }
  }
}
