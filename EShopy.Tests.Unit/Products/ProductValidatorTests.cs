using EShopy.Application.Products.Commands;
using EShopy.Domain.Products;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Products;

public sealed class ProductValidatorTests
{
  // ─── CreateProductCommandValidator ────────────────────────────────────────

  [Fact]
  public void CreateValidator_ShouldFailWhenSlugMissing()
  {
    var validator = new CreateProductCommandValidator();
    var command = new CreateProductCommand("", null, "Coffee", null, 10, 1);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Slug));
  }

  [Fact]
  public void CreateValidator_ShouldFailWhenSlugExceedsMaxLength()
  {
    var validator = new CreateProductCommandValidator();
    var longSlug = new string('a', 129);
    var command = new CreateProductCommand(longSlug, null, "Coffee", null, 10, 1);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Slug));
  }

  [Fact]
  public void CreateValidator_ShouldFailWhenSlugHasUpperCase()
  {
    var validator = new CreateProductCommandValidator();
    var command = new CreateProductCommand("Coffee-Mug", null, "Coffee", null, 10, 1);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Slug));
  }

  [Fact]
  public void CreateValidator_ShouldFailWhenPriceNegative()
  {
    var validator = new CreateProductCommandValidator();
    var command = new CreateProductCommand("coffee", null, "Coffee", null, -1, 1);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Price));
  }

  [Fact]
  public void CreateValidator_ShouldFailWhenStockNegative()
  {
    var validator = new CreateProductCommandValidator();
    var command = new CreateProductCommand("coffee", null, "Coffee", null, 10, -1);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.StockOnHand));
  }

  [Fact]
  public void CreateValidator_ShouldPassWithValidData()
  {
    var validator = new CreateProductCommandValidator();
    var command = new CreateProductCommand("coffee-mug", "MUG-001", "Coffee Mug", "Descripción", 15000, 10);

    var result = validator.Validate(command);

    result.IsValid.Should().BeTrue();
  }

  // ─── UpdateProductCommandValidator ────────────────────────────────────────

  [Fact]
  public void UpdateValidator_ShouldFailWhenPriceNegative()
  {
    var validator = new UpdateProductCommandValidator();
    var command = new UpdateProductCommand(Guid.NewGuid(), "Coffee", null, -5, 1, null);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Price));
  }

  [Fact]
  public void UpdateValidator_ShouldFailWhenNameEmpty()
  {
    var validator = new UpdateProductCommandValidator();
    var command = new UpdateProductCommand(Guid.NewGuid(), "", null, 10, 1, null);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
  }

  // ─── ChangeProductStatusCommandValidator ──────────────────────────────────

  [Fact]
  public void StatusValidator_ShouldAllowKnownStatus()
  {
    var validator = new ChangeProductStatusCommandValidator();
    var command = new ChangeProductStatusCommand(Guid.NewGuid(), ProductStatus.Active);

    var result = validator.Validate(command);

    result.IsValid.Should().BeTrue();
  }

  [Fact]
  public void StatusValidator_ShouldFailForInvalidEnumValue()
  {
    var validator = new ChangeProductStatusCommandValidator();
    var command = new ChangeProductStatusCommand(Guid.NewGuid(), (ProductStatus)99);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
  }
}
