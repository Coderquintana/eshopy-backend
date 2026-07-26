using EShopy.Application.Orders.Commands;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Orders;

public sealed class CheckoutCommandValidatorTests
{
  private readonly CheckoutCommandValidator _validator = new();

  [Fact]
  public void Validate_ShouldFailWhenBuyerEmailEmpty()
  {
    var command = new CheckoutCommand("", "Buyer Name", null);

    var result = _validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutCommand.BuyerEmail));
  }

  [Fact]
  public void Validate_ShouldFailWhenBuyerEmailIsNotValidFormat()
  {
    var command = new CheckoutCommand("not-an-email", "Buyer Name", null);

    var result = _validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutCommand.BuyerEmail));
  }

  [Fact]
  public void Validate_ShouldFailWhenBuyerNameEmpty()
  {
    var command = new CheckoutCommand("buyer@eshopy.local", "", null);

    var result = _validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutCommand.BuyerName));
  }

  [Fact]
  public void Validate_ShouldFailWhenShippingAddressTooLong()
  {
    var command = new CheckoutCommand("buyer@eshopy.local", "Buyer Name", new string('a', 1001));

    var result = _validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutCommand.ShippingAddress));
  }

  [Fact]
  public void Validate_ShouldPassWithValidDataAndNoShippingAddress()
  {
    var command = new CheckoutCommand("buyer@eshopy.local", "Buyer Name", null);

    var result = _validator.Validate(command);

    result.IsValid.Should().BeTrue();
  }

  [Fact]
  public void Validate_ShouldPassWithValidShippingAddress()
  {
    var command = new CheckoutCommand("buyer@eshopy.local", "Buyer Name", "Calle Falsa 123");

    var result = _validator.Validate(command);

    result.IsValid.Should().BeTrue();
  }
}
