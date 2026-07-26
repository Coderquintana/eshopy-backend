using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Subscriptions;

public sealed class SubscriptionTests
{
  private static readonly Guid TenantId = Guid.NewGuid();

  // ─── CreatePending ────────────────────────────────────────────────────────

  [Fact]
  public void CreatePending_WithValidData_ShouldReturnPendingActivationSubscription()
  {
    var now = new DateTime(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc);

    var subscription = Subscription.CreatePending(TenantId, TenantPlan.Basic, 0m, "PYG", now);

    subscription.Status.Should().Be(SubscriptionStatus.PendingActivation);
    subscription.Plan.Should().Be(TenantPlan.Basic);
    subscription.BillingCycleStart.Should().Be(now);
    subscription.BillingCycleEnd.Should().Be(now);
  }

  [Fact]
  public void CreatePending_WithNegativePrice_ShouldThrowDomainException()
  {
    var act = () => Subscription.CreatePending(TenantId, TenantPlan.Basic, -1m, "PYG", DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  // ─── ChangeStatus ─────────────────────────────────────────────────────────

  [Fact]
  public void ChangeStatus_PendingActivationToActive_ShouldRecalculateBillingCycle()
  {
    var subscription = Subscription.CreatePending(TenantId, TenantPlan.Basic, 0m, "PYG", DateTime.UtcNow);
    var activatedAt = DateTime.UtcNow;

    subscription.ChangeStatus(SubscriptionStatus.Active, activatedAt);

    subscription.Status.Should().Be(SubscriptionStatus.Active);
    subscription.BillingCycleStart.Should().Be(activatedAt);
    subscription.BillingCycleEnd.Should().Be(activatedAt.AddMonths(1));
  }

  [Fact]
  public void ChangeStatus_ActiveToPastDue_ShouldSucceed()
  {
    var subscription = Subscription.CreatePending(TenantId, TenantPlan.Basic, 0m, "PYG", DateTime.UtcNow);
    subscription.ChangeStatus(SubscriptionStatus.Active, DateTime.UtcNow);

    subscription.ChangeStatus(SubscriptionStatus.PastDue, DateTime.UtcNow);

    subscription.Status.Should().Be(SubscriptionStatus.PastDue);
  }

  [Fact]
  public void ChangeStatus_PastDueToSuspended_ShouldSucceed()
  {
    var subscription = Subscription.CreatePending(TenantId, TenantPlan.Basic, 0m, "PYG", DateTime.UtcNow);
    subscription.ChangeStatus(SubscriptionStatus.Active, DateTime.UtcNow);
    subscription.ChangeStatus(SubscriptionStatus.PastDue, DateTime.UtcNow);

    subscription.ChangeStatus(SubscriptionStatus.Suspended, DateTime.UtcNow);

    subscription.Status.Should().Be(SubscriptionStatus.Suspended);
  }

  [Fact]
  public void ChangeStatus_SuspendedToCancelled_ShouldStampCancelledAt()
  {
    var subscription = Subscription.CreatePending(TenantId, TenantPlan.Basic, 0m, "PYG", DateTime.UtcNow);
    subscription.ChangeStatus(SubscriptionStatus.Active, DateTime.UtcNow);
    subscription.ChangeStatus(SubscriptionStatus.PastDue, DateTime.UtcNow);
    subscription.ChangeStatus(SubscriptionStatus.Suspended, DateTime.UtcNow);
    var cancelledAt = DateTime.UtcNow;

    subscription.ChangeStatus(SubscriptionStatus.Cancelled, cancelledAt);

    subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    subscription.CancelledAtUtc.Should().Be(cancelledAt);
  }

  [Fact]
  public void ChangeStatus_PastDueToCancelled_ShouldThrowDomainException()
  {
    var subscription = Subscription.CreatePending(TenantId, TenantPlan.Basic, 0m, "PYG", DateTime.UtcNow);
    subscription.ChangeStatus(SubscriptionStatus.Active, DateTime.UtcNow);
    subscription.ChangeStatus(SubscriptionStatus.PastDue, DateTime.UtcNow);

    var act = () => subscription.ChangeStatus(SubscriptionStatus.Cancelled, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.TenantInvalidState);
  }

  [Fact]
  public void ChangeStatus_PendingActivationToPastDue_ShouldThrowDomainException()
  {
    var subscription = Subscription.CreatePending(TenantId, TenantPlan.Basic, 0m, "PYG", DateTime.UtcNow);

    var act = () => subscription.ChangeStatus(SubscriptionStatus.PastDue, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.TenantInvalidState);
  }
}
