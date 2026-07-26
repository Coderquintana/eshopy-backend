using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Payments;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Payments;

public sealed class PaymentTests
{
  private static readonly Guid TenantId = Guid.NewGuid();
  private static readonly Guid OrderId = Guid.NewGuid();
  private static readonly DateTime Now = new(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc);

  // ─── CreateInitiated ──────────────────────────────────────────────────────

  [Fact]
  public void CreateInitiated_WithValidData_ShouldStartInInitiatedStatus()
  {
    var payment = Payment.CreateInitiated(TenantId, OrderId, "fake", 2000m, "PYG",
      "provider-payment-id", "https://fake-payment.local/pay/1", Now);

    payment.Status.Should().Be(PaymentStatus.Initiated);
    payment.OrderId.Should().Be(OrderId);
    payment.Amount.Should().Be(2000m);
    payment.ErrorCode.Should().BeNull();
  }

  [Fact]
  public void CreateInitiated_WithEmptyProvider_ShouldThrowDomainException()
  {
    var act = () => Payment.CreateInitiated(TenantId, OrderId, "", 2000m, "PYG",
      "provider-payment-id", "https://fake-payment.local/pay/1", Now);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  // ─── ChangeStatus ─────────────────────────────────────────────────────────

  [Theory]
  [InlineData(PaymentStatus.Initiated, PaymentStatus.Authorized)]
  [InlineData(PaymentStatus.Initiated, PaymentStatus.Failed)]
  [InlineData(PaymentStatus.Authorized, PaymentStatus.Captured)]
  [InlineData(PaymentStatus.Authorized, PaymentStatus.Failed)]
  [InlineData(PaymentStatus.Captured, PaymentStatus.Refunded)]
  public void ChangeStatus_WithAllowedTransition_ShouldSucceed(PaymentStatus from, PaymentStatus to)
  {
    var payment = CreatePaymentInStatus(from);

    payment.ChangeStatus(to, Now.AddMinutes(1));

    payment.Status.Should().Be(to);
  }

  [Theory]
  [InlineData(PaymentStatus.Initiated, PaymentStatus.Captured)]
  [InlineData(PaymentStatus.Initiated, PaymentStatus.Refunded)]
  [InlineData(PaymentStatus.Failed, PaymentStatus.Authorized)]
  [InlineData(PaymentStatus.Refunded, PaymentStatus.Captured)]
  public void ChangeStatus_WithDisallowedTransition_ShouldThrowDomainException(PaymentStatus from, PaymentStatus to)
  {
    var payment = CreatePaymentInStatus(from);

    var act = () => payment.ChangeStatus(to, Now.AddMinutes(1));

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.OrderInvalidState);
  }

  [Fact]
  public void ChangeStatus_ToFailed_ShouldRecordErrorDetails()
  {
    var payment = Payment.CreateInitiated(TenantId, OrderId, "fake", 2000m, "PYG", null, null, Now);

    payment.ChangeStatus(PaymentStatus.Failed, Now.AddMinutes(1), "DECLINED", "Fondos insuficientes.");

    payment.ErrorCode.Should().Be("DECLINED");
    payment.ErrorMessage.Should().Be("Fondos insuficientes.");
  }

  private static Payment CreatePaymentInStatus(PaymentStatus status)
  {
    var payment = Payment.CreateInitiated(TenantId, OrderId, "fake", 2000m, "PYG",
      "provider-payment-id", "https://fake-payment.local/pay/1", Now);

    switch (status)
    {
      case PaymentStatus.Initiated:
        break;
      case PaymentStatus.Authorized:
        payment.ChangeStatus(PaymentStatus.Authorized, Now);
        break;
      case PaymentStatus.Captured:
        payment.ChangeStatus(PaymentStatus.Authorized, Now);
        payment.ChangeStatus(PaymentStatus.Captured, Now);
        break;
      case PaymentStatus.Failed:
        payment.ChangeStatus(PaymentStatus.Failed, Now);
        break;
      case PaymentStatus.Refunded:
        payment.ChangeStatus(PaymentStatus.Authorized, Now);
        payment.ChangeStatus(PaymentStatus.Captured, Now);
        payment.ChangeStatus(PaymentStatus.Refunded, Now);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(status));
    }

    return payment;
  }
}
