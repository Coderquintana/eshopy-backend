using EShopy.Domain.Carts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Carts;

public sealed class CartTests
{
  private static readonly Guid TenantId = Guid.NewGuid();
  private static readonly Guid ProductId = Guid.NewGuid();

  // ─── Create ───────────────────────────────────────────────────────────────

  [Fact]
  public void Create_WithValidData_ShouldReturnEmptyCart()
  {
    var now = new DateTime(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc);

    var cart = Cart.Create(TenantId, "cart-token-123", now);

    cart.TenantId.Should().Be(TenantId);
    cart.CartToken.Should().Be("cart-token-123");
    cart.Items.Should().BeEmpty();
    cart.ExpiresAtUtc.Should().BeAfter(now);
  }

  [Fact]
  public void Create_WithEmptyCartToken_ShouldThrowDomainException()
  {
    var act = () => Cart.Create(TenantId, "", DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  // ─── AddItem ──────────────────────────────────────────────────────────────

  [Fact]
  public void AddItem_NewProduct_ShouldAddSingleItem()
  {
    var cart = Cart.Create(TenantId, "token", DateTime.UtcNow);

    cart.AddItem(ProductId, 2, DateTime.UtcNow);

    cart.Items.Should().ContainSingle(i => i.ProductId == ProductId && i.Quantity == 2);
  }

  [Fact]
  public void AddItem_ExistingProduct_ShouldAccumulateQuantityNotDuplicate()
  {
    var cart = Cart.Create(TenantId, "token", DateTime.UtcNow);
    cart.AddItem(ProductId, 2, DateTime.UtcNow);

    cart.AddItem(ProductId, 3, DateTime.UtcNow);

    cart.Items.Should().ContainSingle();
    cart.Items[0].Quantity.Should().Be(5);
  }

  [Fact]
  public void AddItem_WithZeroQuantity_ShouldThrowDomainException()
  {
    var cart = Cart.Create(TenantId, "token", DateTime.UtcNow);

    var act = () => cart.AddItem(ProductId, 0, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.ValidationError);
  }

  [Fact]
  public void AddItem_ShouldExtendExpiration()
  {
    var now = DateTime.UtcNow;
    var cart = Cart.Create(TenantId, "token", now);
    var expirationAtCreation = cart.ExpiresAtUtc;

    var later = now.AddDays(10);
    cart.AddItem(ProductId, 1, later);

    cart.ExpiresAtUtc.Should().BeAfter(expirationAtCreation);
  }

  // ─── UpdateItemQuantity ───────────────────────────────────────────────────

  [Fact]
  public void UpdateItemQuantity_ExistingItem_ShouldSetNewQuantity()
  {
    var cart = Cart.Create(TenantId, "token", DateTime.UtcNow);
    cart.AddItem(ProductId, 2, DateTime.UtcNow);

    cart.UpdateItemQuantity(ProductId, 10, DateTime.UtcNow);

    cart.Items[0].Quantity.Should().Be(10);
  }

  [Fact]
  public void UpdateItemQuantity_ItemNotInCart_ShouldThrowDomainException()
  {
    var cart = Cart.Create(TenantId, "token", DateTime.UtcNow);

    var act = () => cart.UpdateItemQuantity(ProductId, 1, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.NotFound);
  }

  // ─── RemoveItem ───────────────────────────────────────────────────────────

  [Fact]
  public void RemoveItem_ExistingItem_ShouldRemoveIt()
  {
    var cart = Cart.Create(TenantId, "token", DateTime.UtcNow);
    cart.AddItem(ProductId, 1, DateTime.UtcNow);

    cart.RemoveItem(ProductId, DateTime.UtcNow);

    cart.Items.Should().BeEmpty();
  }

  [Fact]
  public void RemoveItem_ItemNotInCart_ShouldThrowDomainException()
  {
    var cart = Cart.Create(TenantId, "token", DateTime.UtcNow);

    var act = () => cart.RemoveItem(ProductId, DateTime.UtcNow);

    act.Should().Throw<DomainException>()
      .Where(ex => ex.Code == ErrorCodes.NotFound);
  }
}
