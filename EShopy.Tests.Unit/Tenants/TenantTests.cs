using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Tenants;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Tenants;

public sealed class TenantTests
{
  // ─── Create ───────────────────────────────────────────────────────────────

  [Fact]
  public void Create_WithValidData_ShouldReturnPendingPaymentTenant()
  {
    var now = new DateTime(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc);

    var tenant = Tenant.Create("mitienda", "Mi Tienda SRL", TenantPlan.Basic, now);

    tenant.Subdomain.Should().Be("mitienda");
    tenant.BusinessName.Should().Be("Mi Tienda SRL");
    tenant.Status.Should().Be(TenantStatus.PendingPayment);
    tenant.Plan.Should().Be(TenantPlan.Basic);
    tenant.CreatedAtUtc.Should().Be(now);
    tenant.ActivatedAtUtc.Should().BeNull();
  }

  [Fact]
  public void Create_SubdomainShouldBeNormalized()
  {
    var tenant = Tenant.Create("  MiTienda  ", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);

    tenant.Subdomain.Should().Be("mitienda");
  }

  [Fact]
  public void Create_WithTooShortSubdomain_ShouldThrowDomainException()
  {
    var act = () => Tenant.Create("ab", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  [Fact]
  public void Create_WithInvalidCharactersInSubdomain_ShouldThrowDomainException()
  {
    var act = () => Tenant.Create("mi tienda!", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  [Fact]
  public void Create_WithEmptyBusinessName_ShouldThrowDomainException()
  {
    var act = () => Tenant.Create("mitienda", "", TenantPlan.Basic, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  // ─── ChangeStatus ─────────────────────────────────────────────────────────

  [Fact]
  public void ChangeStatus_PendingPaymentToActive_ShouldSucceedAndStampActivatedAt()
  {
    var tenant = Tenant.Create("mitienda", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);
    var activatedAt = DateTime.UtcNow;

    tenant.ChangeStatus(TenantStatus.Active, activatedAt);

    tenant.Status.Should().Be(TenantStatus.Active);
    tenant.ActivatedAtUtc.Should().Be(activatedAt);
  }

  [Fact]
  public void ChangeStatus_ActiveToSuspended_ShouldSucceed()
  {
    var tenant = Tenant.Create("mitienda", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);
    tenant.ChangeStatus(TenantStatus.Active, DateTime.UtcNow);

    tenant.ChangeStatus(TenantStatus.Suspended, DateTime.UtcNow);

    tenant.Status.Should().Be(TenantStatus.Suspended);
  }

  [Fact]
  public void ChangeStatus_SuspendedToActive_ShouldSucceedAndKeepOriginalActivatedAt()
  {
    var tenant = Tenant.Create("mitienda", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);
    var firstActivation = DateTime.UtcNow;
    tenant.ChangeStatus(TenantStatus.Active, firstActivation);
    tenant.ChangeStatus(TenantStatus.Suspended, DateTime.UtcNow);

    tenant.ChangeStatus(TenantStatus.Active, DateTime.UtcNow.AddDays(1));

    tenant.Status.Should().Be(TenantStatus.Active);
    tenant.ActivatedAtUtc.Should().Be(firstActivation);
  }

  [Fact]
  public void ChangeStatus_ActiveToCancelled_ShouldSucceed()
  {
    var tenant = Tenant.Create("mitienda", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);
    tenant.ChangeStatus(TenantStatus.Active, DateTime.UtcNow);

    tenant.ChangeStatus(TenantStatus.Cancelled, DateTime.UtcNow);

    tenant.Status.Should().Be(TenantStatus.Cancelled);
  }

  [Fact]
  public void ChangeStatus_SuspendedToCancelled_ShouldSucceed()
  {
    var tenant = Tenant.Create("mitienda", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);
    tenant.ChangeStatus(TenantStatus.Active, DateTime.UtcNow);
    tenant.ChangeStatus(TenantStatus.Suspended, DateTime.UtcNow);

    tenant.ChangeStatus(TenantStatus.Cancelled, DateTime.UtcNow);

    tenant.Status.Should().Be(TenantStatus.Cancelled);
  }

  [Fact]
  public void ChangeStatus_PendingPaymentToSuspended_ShouldThrowDomainException()
  {
    var tenant = Tenant.Create("mitienda", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);

    var act = () => tenant.ChangeStatus(TenantStatus.Suspended, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.TenantInvalidState);
  }

  [Fact]
  public void ChangeStatus_CancelledToActive_ShouldThrowDomainException()
  {
    var tenant = Tenant.Create("mitienda", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);
    tenant.ChangeStatus(TenantStatus.Active, DateTime.UtcNow);
    tenant.ChangeStatus(TenantStatus.Cancelled, DateTime.UtcNow);

    var act = () => tenant.ChangeStatus(TenantStatus.Active, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.TenantInvalidState);
  }

  [Fact]
  public void ChangeStatus_SameStatus_ShouldBeIdempotent()
  {
    var tenant = Tenant.Create("mitienda", "Mi Tienda SRL", TenantPlan.Basic, DateTime.UtcNow);
    var updatedBefore = tenant.UpdatedAtUtc;

    tenant.ChangeStatus(TenantStatus.PendingPayment, DateTime.UtcNow);

    tenant.Status.Should().Be(TenantStatus.PendingPayment);
    tenant.UpdatedAtUtc.Should().Be(updatedBefore);
  }
}
