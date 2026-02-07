namespace EShopy.Application.Products.Contracts;

public sealed class ProductAdminDto
{
  public required Guid Id { get; init; }
  public required string Slug { get; init; }
  public string? Sku { get; init; }
  public required string Name { get; init; }
  public string? Description { get; init; }
  public required decimal Price { get; init; }
  public required string CurrencyCode { get; init; }
  public required string Status { get; init; }
  public required int StockOnHand { get; init; }
  public required DateTime CreatedAtUtc { get; init; }
  public required DateTime UpdatedAtUtc { get; init; }
}
