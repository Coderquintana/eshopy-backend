using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Products;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Products;

public sealed class ProductTests
{
  private static readonly Guid TenantId = Guid.NewGuid();
  private static readonly Guid StoreId = Guid.NewGuid();

  // Helpers
  private static Product CreateDraft(string slug = "coffee-mug", decimal price = 10m, int stock = 5)
    => Product.Create(TenantId, StoreId, slug, null, "Coffee Mug", null, price, stock, "PYG", DateTime.UtcNow);

  // ─── Create ───────────────────────────────────────────────────────────────

  [Fact]
  public void Create_WithValidData_ShouldReturnDraftProduct()
  {
    var now = new DateTime(2026, 2, 5, 19, 0, 0, DateTimeKind.Utc);

    var product = Product.Create(TenantId, StoreId, "coffee-mug", null, "Coffee Mug", "Nice mug", 10.5m, 3, "PYG", now);

    product.TenantId.Should().Be(TenantId);
    product.StoreId.Should().Be(StoreId);
    product.Status.Should().Be(ProductStatus.Draft);
    product.CreatedAtUtc.Should().Be(now);
    product.UpdatedAtUtc.Should().Be(now);
    product.Slug.Should().Be("coffee-mug");
    product.CurrencyCode.Should().Be("PYG");
  }

  [Fact]
  public void Create_WithNegativePrice_ShouldThrowDomainException()
  {
    var act = () => Product.Create(TenantId, StoreId, "coffee-mug", null, "Coffee Mug", null, -1m, 0, "PYG", DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  [Fact]
  public void Create_WithNegativeStock_ShouldThrowDomainException()
  {
    var act = () => Product.Create(TenantId, StoreId, "coffee-mug", null, "Coffee Mug", null, 10m, -5, "PYG", DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  [Fact]
  public void Create_WithZeroPrice_ShouldSucceed()
  {
    // Precio cero es válido (productos gratuitos)
    var product = CreateDraft(price: 0);

    product.Price.Should().Be(0);
  }

  [Fact]
  public void Create_SlugShouldBeNormalized()
  {
    var product = Product.Create(TenantId, StoreId, "  COFFEE-MUG  ", null, "Coffee Mug", null, 10m, 0, "PYG", DateTime.UtcNow);

    product.Slug.Should().Be("coffee-mug");
  }

  // ─── ChangeStatus ─────────────────────────────────────────────────────────

  [Fact]
  public void ChangeStatus_DraftToActive_ShouldSucceed()
  {
    var product = CreateDraft();

    product.ChangeStatus(ProductStatus.Active, DateTime.UtcNow);

    product.Status.Should().Be(ProductStatus.Active);
  }

  [Fact]
  public void ChangeStatus_ActiveToArchived_ShouldSucceed()
  {
    var product = CreateDraft();
    product.ChangeStatus(ProductStatus.Active, DateTime.UtcNow);

    product.ChangeStatus(ProductStatus.Archived, DateTime.UtcNow);

    product.Status.Should().Be(ProductStatus.Archived);
  }

  [Fact]
  public void ChangeStatus_ArchivedToActive_ShouldSucceed()
  {
    var product = CreateDraft();
    product.ChangeStatus(ProductStatus.Active, DateTime.UtcNow);
    product.ChangeStatus(ProductStatus.Archived, DateTime.UtcNow);

    product.ChangeStatus(ProductStatus.Active, DateTime.UtcNow);

    product.Status.Should().Be(ProductStatus.Active);
  }

  [Fact]
  public void ChangeStatus_DraftToArchived_ShouldThrowDomainException()
  {
    var product = CreateDraft();

    var act = () => product.ChangeStatus(ProductStatus.Archived, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ProductInvalidState);
  }

  [Fact]
  public void ChangeStatus_ActiveToDraft_ShouldThrowDomainException()
  {
    var product = CreateDraft();
    product.ChangeStatus(ProductStatus.Active, DateTime.UtcNow);

    var act = () => product.ChangeStatus(ProductStatus.Draft, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ProductInvalidState);
  }

  [Fact]
  public void ChangeStatus_ArchivedToDraft_ShouldThrowDomainException()
  {
    var product = CreateDraft();
    product.ChangeStatus(ProductStatus.Active, DateTime.UtcNow);
    product.ChangeStatus(ProductStatus.Archived, DateTime.UtcNow);

    var act = () => product.ChangeStatus(ProductStatus.Draft, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ProductInvalidState);
  }

  [Fact]
  public void ChangeStatus_SameStatus_ShouldBeIdempotent()
  {
    var product = CreateDraft();
    var updatedBefore = product.UpdatedAtUtc;

    // Cambiar al mismo estado no debe lanzar excepción ni modificar updatedAt
    product.ChangeStatus(ProductStatus.Draft, DateTime.UtcNow);

    product.Status.Should().Be(ProductStatus.Draft);
    product.UpdatedAtUtc.Should().Be(updatedBefore);
  }
}
