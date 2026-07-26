using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Domain.Carts;

/// <summary>
/// Entidad hija de <see cref="Cart"/>: solo se muta a traves del agregado (miembros internal).
/// No guarda precio — el checkout es quien snapshotea el precio, sobre OrderItem.
/// </summary>
public sealed class CartItem
{
  private CartItem(Guid id, Guid cartId, Guid productId, int quantity, DateTime createdAtUtc, DateTime? updatedAtUtc)
  {
    Id = id;
    CartId = cartId;
    ProductId = productId;
    Quantity = quantity;
    CreatedAtUtc = createdAtUtc;
    UpdatedAtUtc = updatedAtUtc;
  }

  public Guid Id { get; private set; }
  public Guid CartId { get; private set; }
  public Guid ProductId { get; private set; }
  public int Quantity { get; private set; }
  public DateTime CreatedAtUtc { get; private set; }
  public DateTime? UpdatedAtUtc { get; private set; }

  internal static CartItem Create(Guid cartId, Guid productId, int quantity, DateTime createdAtUtc)
  {
    EnsureQuantity(quantity);
    return new CartItem(Guid.NewGuid(), cartId, productId, quantity, createdAtUtc, createdAtUtc);
  }

  internal void IncreaseQuantity(int amount, DateTime updatedAtUtc)
  {
    EnsureQuantity(Quantity + amount);
    Quantity += amount;
    UpdatedAtUtc = updatedAtUtc;
  }

  internal void SetQuantity(int quantity, DateTime updatedAtUtc)
  {
    EnsureQuantity(quantity);
    Quantity = quantity;
    UpdatedAtUtc = updatedAtUtc;
  }

  private static void EnsureQuantity(int quantity)
  {
    if (quantity < 1)
      throw new DomainException(ErrorCodes.ValidationError, "La cantidad debe ser mayor o igual a 1.");
  }
}
