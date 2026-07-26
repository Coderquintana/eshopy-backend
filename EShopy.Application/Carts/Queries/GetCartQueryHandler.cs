using EShopy.Application.Carts.Contracts;
using EShopy.Application.Common.Context;
using EShopy.Application.Common.Stores;
using EShopy.Application.Products;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Carts.Queries;

public sealed class GetCartQueryHandler(
  ICartRepository cartRepository,
  IProductRepository productRepository,
  IStoreService storeService,
  TenantContext tenantContext)
{
  public async Task<Result<CartDto>> Handle(GetCartQuery query, string cartToken, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(cartToken))
      return Result<CartDto>.Fail(ErrorCodes.ValidationError, "El header X-Cart-Token es obligatorio.");

    if (!tenantContext.TenantId.HasValue)
      return Result<CartDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;

    var store = await storeService.GetDefaultStoreAsync(tenantId, ct);
    if (store is null)
      return Result<CartDto>.Fail(ErrorCodes.NotFound, "No existe un store configurado para este tenant.");

    var cart = await cartRepository.GetByCartTokenAsync(tenantId, cartToken, ct);

    // Un carrito que todavia no existe (nunca se le agrego nada) es, para el caller, un carrito vacio.
    var dto = cart is null
      ? CartMappings.ToEmptyDto(cartToken, store.CurrencyCode)
      : await CartMappings.ToDtoAsync(cart, store.CurrencyCode, productRepository, ct);

    return Result<CartDto>.Ok(dto);
  }
}
