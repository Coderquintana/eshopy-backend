namespace EShopy.Application.Products.Contracts;

public sealed class ProductPublicDto
{
  public required Guid Id { get; init; }
  public required string Slug { get; init; }
  public required string Name { get; init; }
  public string? Description { get; init; }
  public required decimal Price { get; init; }
  public required string CurrencyCode { get; init; }
}
