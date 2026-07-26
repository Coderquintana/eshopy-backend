namespace EShopy.Application.Orders.Contracts;

public sealed class OrderItemDto
{
  public required Guid ProductId { get; init; }
  public required string ProductName { get; init; }
  public string? ProductSku { get; init; }
  public required decimal UnitPrice { get; init; }
  public required int Quantity { get; init; }
  public required decimal Subtotal { get; init; }
}
