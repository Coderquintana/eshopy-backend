namespace EShopy.Application.Products.Requests;

public sealed class UpdateProductRequest
{
  public required string Name { get; init; }
  public string? Description { get; init; }
  public required decimal Price { get; init; }
  public required int StockOnHand { get; init; }
}
