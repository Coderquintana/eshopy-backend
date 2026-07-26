namespace EShopy.Application.Orders.Contracts;

public sealed class CheckoutResultDto
{
  public required Guid OrderId { get; init; }
  public required int OrderNumber { get; init; }
  public required decimal TotalAmount { get; init; }
  public required string CurrencyCode { get; init; }
  public required string PaymentUrl { get; init; }
}
