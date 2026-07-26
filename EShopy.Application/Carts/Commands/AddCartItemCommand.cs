namespace EShopy.Application.Carts.Commands;

public sealed record AddCartItemCommand(Guid ProductId, int Quantity);
