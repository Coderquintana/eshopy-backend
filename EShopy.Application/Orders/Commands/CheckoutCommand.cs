namespace EShopy.Application.Orders.Commands;

public sealed record CheckoutCommand(string BuyerEmail, string BuyerName, string? ShippingAddress);
