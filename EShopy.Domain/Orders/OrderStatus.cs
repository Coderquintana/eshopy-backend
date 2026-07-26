namespace EShopy.Domain.Orders;

public enum OrderStatus : byte
{
  PendingPayment = 0,
  Paid = 1,
  Cancelled = 2,
  Refunded = 3
}
