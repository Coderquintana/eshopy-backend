namespace EShopy.Application.Orders.Contracts;

public sealed class CheckoutResultDto
{
  public required Guid OrderId { get; init; }
  public required int OrderNumber { get; init; }
  public required decimal TotalAmount { get; init; }
  public required string CurrencyCode { get; init; }
  public required string PaymentUrl { get; init; }

  /// <summary>
  /// Secreto de un solo pedido para consultarlo luego via GET /api/public/orders/{orderId}
  /// (header X-Order-Token). El frontend debe persistirlo ANTES de redirigir al provider de pago:
  /// es la unica vez que el backend lo devuelve.
  /// </summary>
  public required string AccessToken { get; init; }
}
