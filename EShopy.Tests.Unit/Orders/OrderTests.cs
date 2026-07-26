using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Orders;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Orders;

public sealed class OrderTests
{
  private static readonly Guid TenantId = Guid.NewGuid();
  private static readonly Guid StoreId = Guid.NewGuid();
  private static readonly DateTime Now = new(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc);

  private static IReadOnlyList<OrderItemData> OneItem(decimal unitPrice = 1000m, int quantity = 2)
    => [new OrderItemData(Guid.NewGuid(), "Producto", "SKU-1", unitPrice, quantity)];

  // ─── Create ───────────────────────────────────────────────────────────────

  [Fact]
  public void Create_WithValidData_ShouldSumItemsIntoTotalAmount()
  {
    var order = Order.Create(TenantId, StoreId, "buyer@eshopy.local", "Buyer Name", null,
      "cart-token", OneItem(1000m, 2), "PYG", Now);

    order.TotalAmount.Should().Be(2000m);
    order.OrderNumber.Should().Be(0);
    order.Status.Should().Be(OrderStatus.PendingPayment);
    order.PaymentId.Should().BeNull();
    order.Items.Should().ContainSingle();
  }

  [Fact]
  public void Create_WithNoItems_ShouldThrowDomainException()
  {
    var act = () => Order.Create(TenantId, StoreId, "buyer@eshopy.local", "Buyer Name", null,
      "cart-token", [], "PYG", Now);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  [Fact]
  public void Create_WithEmptyBuyerEmail_ShouldThrowDomainException()
  {
    var act = () => Order.Create(TenantId, StoreId, "", "Buyer Name", null,
      "cart-token", OneItem(), "PYG", Now);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  // ─── AssignOrderNumber ────────────────────────────────────────────────────

  [Fact]
  public void AssignOrderNumber_FirstCall_ShouldSetValue()
  {
    var order = Order.Create(TenantId, StoreId, "buyer@eshopy.local", "Buyer Name", null,
      "cart-token", OneItem(), "PYG", Now);

    order.AssignOrderNumber(42);

    order.OrderNumber.Should().Be(42);
  }

  [Fact]
  public void AssignOrderNumber_SecondCall_ShouldOverwritePreviousValue()
  {
    // Idempotente a proposito: EfCheckoutWriter reintenta sobre la misma instancia bajo contencion
    // real (ver comentario en Order.AssignOrderNumber) — solo el ultimo valor debe prevalecer.
    var order = Order.Create(TenantId, StoreId, "buyer@eshopy.local", "Buyer Name", null,
      "cart-token", OneItem(), "PYG", Now);
    order.AssignOrderNumber(1);

    order.AssignOrderNumber(2);

    order.OrderNumber.Should().Be(2);
  }

  // ─── ChangeStatus ─────────────────────────────────────────────────────────

  [Theory]
  [InlineData(OrderStatus.PendingPayment, OrderStatus.Paid)]
  [InlineData(OrderStatus.PendingPayment, OrderStatus.Cancelled)]
  [InlineData(OrderStatus.Paid, OrderStatus.Refunded)]
  public void ChangeStatus_WithAllowedTransition_ShouldSucceed(OrderStatus from, OrderStatus to)
  {
    var order = CreateOrderInStatus(from);

    order.ChangeStatus(to, Now.AddMinutes(1));

    order.Status.Should().Be(to);
  }

  [Theory]
  [InlineData(OrderStatus.PendingPayment, OrderStatus.Refunded)]
  [InlineData(OrderStatus.Cancelled, OrderStatus.Paid)]
  [InlineData(OrderStatus.Refunded, OrderStatus.Paid)]
  public void ChangeStatus_WithDisallowedTransition_ShouldThrowDomainException(OrderStatus from, OrderStatus to)
  {
    var order = CreateOrderInStatus(from);

    var act = () => order.ChangeStatus(to, Now.AddMinutes(1));

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.OrderInvalidState);
  }

  private static Order CreateOrderInStatus(OrderStatus status)
  {
    var order = Order.Create(TenantId, StoreId, "buyer@eshopy.local", "Buyer Name", null,
      "cart-token", OneItem(), "PYG", Now);

    switch (status)
    {
      case OrderStatus.PendingPayment:
        break;
      case OrderStatus.Paid:
        order.ChangeStatus(OrderStatus.Paid, Now);
        break;
      case OrderStatus.Cancelled:
        order.ChangeStatus(OrderStatus.Cancelled, Now);
        break;
      case OrderStatus.Refunded:
        order.ChangeStatus(OrderStatus.Paid, Now);
        order.ChangeStatus(OrderStatus.Refunded, Now);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(status));
    }

    return order;
  }

  [Fact]
  public void ChangeStatus_ToSameStatus_ShouldBeNoOp()
  {
    var order = Order.Create(TenantId, StoreId, "buyer@eshopy.local", "Buyer Name", null,
      "cart-token", OneItem(), "PYG", Now);

    order.ChangeStatus(OrderStatus.PendingPayment, Now.AddMinutes(1));

    order.Status.Should().Be(OrderStatus.PendingPayment);
    order.UpdatedAtUtc.Should().Be(Now);
  }
}
