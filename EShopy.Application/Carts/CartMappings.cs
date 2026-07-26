using EShopy.Application.Carts.Contracts;
using EShopy.Application.Products;
using EShopy.Domain.Carts;
using EShopy.Domain.Products;

namespace EShopy.Application.Carts;

/// <summary>
/// Arma CartDto uniendo Cart (dominio) con datos de Product en vivo — no es un mapeo puro 1:1
/// como ProductMappings/TenantMappings, necesita leer el catalogo para nombre/slug/precio actual.
/// </summary>
internal static class CartMappings
{
  internal static async Task<CartDto> ToDtoAsync(
    Cart cart, string currencyCode, IProductRepository productRepository, CancellationToken ct)
  {
    var productIds = cart.Items.Select(i => i.ProductId).ToList();
    var products = await productRepository.GetByIdsAsync(cart.TenantId, productIds, ct);
    var productsById = products.ToDictionary(p => p.Id);

    var items = cart.Items
      .Where(i => productsById.ContainsKey(i.ProductId))
      .Select(i => ToItemDto(i, productsById[i.ProductId]))
      .ToList();

    return new CartDto
    {
      CartToken = cart.CartToken,
      Items = items,
      Subtotal = items.Sum(i => i.Subtotal),
      CurrencyCode = currencyCode
    };
  }

  internal static CartDto ToEmptyDto(string cartToken, string currencyCode) => new()
  {
    CartToken = cartToken,
    Items = [],
    Subtotal = 0,
    CurrencyCode = currencyCode
  };

  private static CartItemDto ToItemDto(CartItem item, Product product) => new()
  {
    ProductId = product.Id,
    ProductName = product.Name,
    ProductSlug = product.Slug,
    UnitPrice = product.Price,
    Quantity = item.Quantity,
    Subtotal = product.Price * item.Quantity
  };
}
