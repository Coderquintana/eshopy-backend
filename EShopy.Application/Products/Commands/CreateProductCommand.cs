namespace EShopy.Application.Products.Commands;

/// <summary>Comando para crear un producto en estado Draft.</summary>
/// <remarks>StoreId y CurrencyCode son resueltos por el handler vía IStoreService.</remarks>
public sealed record CreateProductCommand(
  string Slug,
  string? Sku,
  string Name,
  string? Description,
  decimal Price,
  int StockOnHand);
