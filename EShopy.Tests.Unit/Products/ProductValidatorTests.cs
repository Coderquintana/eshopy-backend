using EShopy.Application.Products.Requests;
using EShopy.Application.Products.Validation;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Products;

public sealed class ProductValidatorTests
{
  [Fact]
  public void CreateValidator_ShouldFailWhenSlugMissing()
  {
    var validator = new CreateProductRequestValidator();
    var request = new CreateProductRequest
    {
      Slug = "",
      Name = "Coffee",
      Price = 10,
      StockOnHand = 1
    };

    var result = validator.Validate(request);

    result.IsValid.Should().BeFalse();
  }

  [Fact]
  public void UpdateValidator_ShouldFailWhenPriceNegative()
  {
    var validator = new UpdateProductRequestValidator();
    var request = new UpdateProductRequest
    {
      Name = "Coffee",
      Price = -5,
      StockOnHand = 1
    };

    var result = validator.Validate(request);

    result.IsValid.Should().BeFalse();
  }

  [Fact]
  public void StatusValidator_ShouldAllowKnownStatus()
  {
    var validator = new ChangeProductStatusRequestValidator();
    var request = new ChangeProductStatusRequest
    {
      Status = EShopy.Domain.Products.ProductStatus.Active
    };

    var result = validator.Validate(request);

    result.IsValid.Should().BeTrue();
  }
}
