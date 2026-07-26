namespace EShopy.Application.Carts.Contracts;

public sealed class CartDto
{
  public required string CartToken { get; init; }
  public required IReadOnlyList<CartItemDto> Items { get; init; }
  public required decimal Subtotal { get; init; }
  public required string CurrencyCode { get; init; }
}
