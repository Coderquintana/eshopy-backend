using EShopy.Application.Carts.Commands;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Carts;

public sealed class CartValidatorTests
{
  // ─── AddCartItemCommandValidator ─────────────────────────────────────────

  [Fact]
  public void AddValidator_ShouldFailWhenProductIdEmpty()
  {
    var validator = new AddCartItemCommandValidator();
    var command = new AddCartItemCommand(Guid.Empty, 1);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(AddCartItemCommand.ProductId));
  }

  [Fact]
  public void AddValidator_ShouldFailWhenQuantityZero()
  {
    var validator = new AddCartItemCommandValidator();
    var command = new AddCartItemCommand(Guid.NewGuid(), 0);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(AddCartItemCommand.Quantity));
  }

  [Fact]
  public void AddValidator_ShouldPassWithValidData()
  {
    var validator = new AddCartItemCommandValidator();
    var command = new AddCartItemCommand(Guid.NewGuid(), 3);

    var result = validator.Validate(command);

    result.IsValid.Should().BeTrue();
  }

  // ─── UpdateCartItemQuantityCommandValidator ──────────────────────────────

  [Fact]
  public void UpdateValidator_ShouldFailWhenQuantityNegative()
  {
    var validator = new UpdateCartItemQuantityCommandValidator();
    var command = new UpdateCartItemQuantityCommand(Guid.NewGuid(), -1);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
  }

  [Fact]
  public void UpdateValidator_ShouldPassWithValidData()
  {
    var validator = new UpdateCartItemQuantityCommandValidator();
    var command = new UpdateCartItemQuantityCommand(Guid.NewGuid(), 5);

    var result = validator.Validate(command);

    result.IsValid.Should().BeTrue();
  }
}
