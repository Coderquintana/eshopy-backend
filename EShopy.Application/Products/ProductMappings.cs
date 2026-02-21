using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;

namespace EShopy.Application.Products;

/// <summary>Mapeos compartidos entre Product (dominio) y sus DTOs de aplicación.</summary>
internal static class ProductMappings
{
  internal static ProductAdminDto ToAdminDto(Product product) => new()
  {
    Id = product.Id,
    Slug = product.Slug,
    Sku = product.Sku,
    Name = product.Name,
    Description = product.Description,
    Price = product.Price,
    CurrencyCode = product.CurrencyCode,
    Status = product.Status.ToString(),
    StockOnHand = product.StockOnHand,
    CreatedAtUtc = product.CreatedAtUtc,
    UpdatedAtUtc = product.UpdatedAtUtc
  };

  internal static ProductPublicDto ToPublicDto(Product product) => new()
  {
    Id = product.Id,
    Slug = product.Slug,
    Name = product.Name,
    Description = product.Description,
    Price = product.Price,
    CurrencyCode = product.CurrencyCode
  };
}
