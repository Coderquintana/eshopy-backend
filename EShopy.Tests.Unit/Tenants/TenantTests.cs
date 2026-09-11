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

  // ─── Store ────────────────────────────────────────────────────────────────

  [Fact]
  public void StoreCreateDefault_ShouldNormalizeCurrencyCode()
  {
    var store = Store.CreateDefault(Guid.NewGuid(), "Mi Tienda", "usd", DateTime.UtcNow);

    store.CurrencyCode.Should().Be("USD");
  }

  [Theory]
  [InlineData("")]
  [InlineData("US")]
  [InlineData("US1")]
  [InlineData("EURO")]
  public void StoreCreateDefault_WithInvalidCurrencyCode_ShouldThrowDomainException(string currencyCode)
  {
    var act = () => Store.CreateDefault(Guid.NewGuid(), "Mi Tienda", currencyCode, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  [Fact]
  public void StoreUpdateContactInformation_ShouldNormalizePublicBusinessData()
  {
    var store = Store.CreateDefault(Guid.NewGuid(), "Mi Tienda", "PYG", DateTime.UtcNow);

    store.UpdateContactInformation(
      "  +595 981 123456  ",
      "  ventas@mitienda.com  ",
      "  https://instagram.com/mitienda  ",
      "  https://facebook.com/mitienda  ",
      "  Asunción, Paraguay  ",
      "  Lun a sáb, 09:00 a 18:00  ",
      DateTime.UtcNow);

    store.ContactWhatsapp.Should().Be("+595 981 123456");
    store.ContactEmail.Should().Be("ventas@mitienda.com");
    store.InstagramUrl.Should().Be("https://instagram.com/mitienda");
    store.FacebookUrl.Should().Be("https://facebook.com/mitienda");
    store.Address.Should().Be("Asunción, Paraguay");
    store.BusinessHours.Should().Be("Lun a sáb, 09:00 a 18:00");
  }

  [Fact]
  public void StoreUpdateTheme_ShouldPersistCanonicalAllowedValuesInData()
  {
    var store = Store.CreateDefault(Guid.NewGuid(), "Mi Tienda", "PYG", DateTime.UtcNow);

    store.UpdateTheme(new StoreTheme("georgia", "LARGE", "Rounded", "spacious"), DateTime.UtcNow);

    store.Theme.Should().Be(new StoreTheme("Georgia", "large", "rounded", "spacious"));
    store.Data.Should().NotBeNullOrWhiteSpace();
  }

  [Fact]
  public void StoreUpdateTheme_WithUnknownValue_ShouldThrowDomainException()
  {
    var store = Store.CreateDefault(Guid.NewGuid(), "Mi Tienda", "PYG", DateTime.UtcNow);

    var act = () => store.UpdateTheme(
      new StoreTheme("Comic Sans MS", "normal", "rounded", "normal"),
      DateTime.UtcNow);

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
