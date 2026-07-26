using EShopy.Domain.Orders;

namespace EShopy.Application.Orders.Commands;

public sealed record ChangeOrderStatusCommand(Guid Id, OrderStatus Status);
