namespace EShopy.Application.Orders.Contracts;

public sealed class OrderAdminDto
{
  public required Guid Id { get; init; }
  public required int OrderNumber { get; init; }
  public required string Status { get; init; }
  public required string BuyerEmail { get; init; }
  public required string BuyerName { get; init; }
  public string? ShippingAddress { get; init; }
  public required decimal TotalAmount { get; init; }
  public required string CurrencyCode { get; init; }
  public required IReadOnlyList<OrderItemDto> Items { get; init; }
  public required DateTime CreatedAtUtc { get; init; }
  public DateTime? UpdatedAtUtc { get; init; }
}
