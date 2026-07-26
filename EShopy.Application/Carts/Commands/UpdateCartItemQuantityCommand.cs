namespace EShopy.Application.Carts.Commands;

public sealed record UpdateCartItemQuantityCommand(Guid ProductId, int Quantity);
