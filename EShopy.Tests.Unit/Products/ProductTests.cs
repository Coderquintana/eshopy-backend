using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Products;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Products;

public sealed class ProductTests
{
  [Fact]
  public void Create_ShouldInitializeDraftProduct()
  {
    var tenantId = Guid.NewGuid();
    var now = new DateTime(2026, 2, 5, 19, 0, 0, DateTimeKind.Utc);

    var product = Product.Create(tenantId, "coffee-mug", null, "Coffee Mug", "Nice mug", 10.5m, 3, "PYG", now);

    product.TenantId.Should().Be(tenantId);
    product.Status.Should().Be(ProductStatus.Draft);
    product.CreatedAtUtc.Should().Be(now);
    product.UpdatedAtUtc.Should().Be(now);
    product.Slug.Should().Be("coffee-mug");
  }

  [Fact]
  public void Create_ShouldRejectNegativePrice()
  {
    var tenantId = Guid.NewGuid();

    var act = () => Product.Create(tenantId, "coffee-mug", null, "Coffee Mug", null, -1m, 0, "PYG", DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  [Fact]
  public void ChangeStatus_ShouldUpdateStatus()
  {
    var product = Product.Create(Guid.NewGuid(), "coffee-mug", null, "Coffee Mug", null, 10m, 0, "PYG", DateTime.UtcNow);

    product.ChangeStatus(ProductStatus.Active, DateTime.UtcNow);

    product.Status.Should().Be(ProductStatus.Active);
  }
}
