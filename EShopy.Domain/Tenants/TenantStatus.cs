namespace EShopy.Domain.Tenants;

public enum TenantStatus : byte
{
  PendingPayment = 0,
  Active = 1,
  Suspended = 2,
  Cancelled = 3
}
