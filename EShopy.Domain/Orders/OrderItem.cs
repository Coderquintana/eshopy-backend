using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Domain.Orders;

/// <summary>
/// Snapshot inmutable de un producto al momento del checkout. Sin TenantId propio (se resuelve via
/// OrderId, igual que CartItem via CartId) y sin AppEntity: nunca se actualiza, solo se crea.
/// </summary>
public sealed class OrderItem
{
  private OrderItem(Guid id, Guid orderId, Guid productId, string productName, string? productSku, decimal unitPrice, int quantity)
  {
    Id = id;
    OrderId = orderId;
    ProductId = productId;
    ProductName = productName;
    ProductSku = productSku;
    UnitPrice = unitPrice;
    Quantity = quantity;
  }

  public Guid Id { get; private set; }
  public Guid OrderId { get; private set; }
  public Guid ProductId { get; private set; }
  public string ProductName { get; private set; }
  public string? ProductSku { get; private set; }
  public decimal UnitPrice { get; private set; }
  public int Quantity { get; private set; }

  /// <summary>Calculado, no persistido (evita que quede desincronizado de UnitPrice*Quantity en DB).</summary>
  public decimal Subtotal => UnitPrice * Quantity;

  internal static OrderItem Create(Guid orderId, OrderItemData data)
  {
    if (data.Quantity < 1)
      throw new DomainException(ErrorCodes.ValidationError, "La cantidad debe ser mayor o igual a 1.");

    if (data.UnitPrice < 0)
      throw new DomainException(ErrorCodes.ValidationError, "El precio unitario debe ser mayor o igual a cero.");

    return new OrderItem(Guid.NewGuid(), orderId, data.ProductId, data.ProductName, data.ProductSku, data.UnitPrice, data.Quantity);
  }
}
