namespace EShopy.Application.Carts.Contracts;

public sealed class CartItemDto
{
  public required Guid ProductId { get; init; }
  public required string ProductName { get; init; }
  public required string ProductSlug { get; init; }

  /// <summary>Leido en vivo desde Product — no es un snapshot (eso ocurre recien en el checkout).</summary>
  public required decimal UnitPrice { get; init; }
  public required int Quantity { get; init; }
  public required decimal Subtotal { get; init; }
}
