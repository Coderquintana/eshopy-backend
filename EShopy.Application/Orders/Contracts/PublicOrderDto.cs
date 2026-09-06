namespace EShopy.Application.Orders.Contracts;

/// <summary>
/// Vista reducida del pedido para el comprador anonimo (pantalla de confirmacion de compra).
/// Deliberadamente SIN datos personales (email, nombre, direccion): quien tiene el AccessToken
/// pudo haberlo obtenido de una URL compartida, y esos datos no hacen falta para confirmar la compra.
/// </summary>
public sealed class PublicOrderDto
{
  public required Guid Id { get; init; }
  public required int OrderNumber { get; init; }
  public required string Status { get; init; }
  public required decimal TotalAmount { get; init; }
  public required string CurrencyCode { get; init; }
  public required IReadOnlyList<OrderItemDto> Items { get; init; }
  public required DateTime CreatedAtUtc { get; init; }
}
