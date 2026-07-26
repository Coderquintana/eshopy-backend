using EShopy.Application.Orders.Contracts;
using EShopy.Domain.Orders;

namespace EShopy.Application.Orders;

internal static class OrderMappings
{
  internal static OrderAdminDto ToAdminDto(Order order) => new()
  {
    Id = order.Id,
    OrderNumber = order.OrderNumber,
    Status = order.Status.ToString(),
    BuyerEmail = order.BuyerEmail,
    BuyerName = order.BuyerName,
    ShippingAddress = order.ShippingAddress,
    TotalAmount = order.TotalAmount,
    CurrencyCode = order.CurrencyCode,
    Items = order.Items.Select(ToItemDto).ToList(),
    CreatedAtUtc = order.CreatedAtUtc,
    UpdatedAtUtc = order.UpdatedAtUtc
  };

  private static OrderItemDto ToItemDto(OrderItem item) => new()
  {
    ProductId = item.ProductId,
    ProductName = item.ProductName,
    ProductSku = item.ProductSku,
    UnitPrice = item.UnitPrice,
    Quantity = item.Quantity,
    Subtotal = item.Subtotal
  };
}
