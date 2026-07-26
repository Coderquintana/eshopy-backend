namespace EShopy.Domain.Subscriptions;

public enum SubscriptionStatus : byte
{
  PendingActivation = 0,
  Active = 1,
  PastDue = 2,
  Suspended = 3,
  Cancelled = 4
}
