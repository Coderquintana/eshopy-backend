namespace EShopy.Domain.Orders;

/// <summary>Snapshot de un item del carrito al momento del checkout (ya con el precio actual del Product).</summary>
public sealed record OrderItemData(Guid ProductId, string ProductName, string? ProductSku, decimal UnitPrice, int Quantity);
