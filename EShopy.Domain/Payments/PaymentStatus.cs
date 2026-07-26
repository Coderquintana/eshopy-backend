namespace EShopy.Domain.Payments;

public enum PaymentStatus : byte
{
  Initiated = 0,
  Authorized = 1,
  Captured = 2,
  Failed = 3,
  Refunded = 4
}
