using EShopy.Domain.Common.Entities;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Domain.Carts;

/// <summary>
/// Carrito server-side. Agregado raiz de <see cref="CartItem"/>: toda mutacion de items pasa por
/// aca, nunca directo sobre CartItem, para mantener el invariante "un producto, una fila".
/// </summary>
public sealed class Cart : AppEntity
{
  private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

  private readonly List<CartItem> _items = [];

  private Cart(Guid id, Guid tenantId, string cartToken, DateTime expiresAtUtc, DateTime createdAtUtc, DateTime? updatedAtUtc)
    : base(id, tenantId, createdAtUtc, createdBy: null, updatedAtUtc, updatedBy: null, data: null)
  {
    CartToken = cartToken;
    ExpiresAtUtc = expiresAtUtc;
  }

  public string CartToken { get; private set; }
  public DateTime ExpiresAtUtc { get; private set; }
  public IReadOnlyList<CartItem> Items => _items;

  public static Cart Create(Guid tenantId, string cartToken, DateTime createdAtUtc)
  {
    var normalizedToken = NormalizeCartToken(cartToken);
    return new Cart(Guid.NewGuid(), tenantId, normalizedToken, createdAtUtc.Add(Ttl), createdAtUtc, createdAtUtc);
  }

  /// <summary>Agrega el producto, o acumula cantidad si ya esta en el carrito.</summary>
  public void AddItem(Guid productId, int quantity, DateTime nowUtc)
  {
    var existing = _items.FirstOrDefault(i => i.ProductId == productId);
    if (existing is not null)
      existing.IncreaseQuantity(quantity, nowUtc);
    else
      _items.Add(CartItem.Create(Id, productId, quantity, nowUtc));

    Touch(nowUtc);
  }

  public void UpdateItemQuantity(Guid productId, int quantity, DateTime nowUtc)
  {
    var item = FindItemOrThrow(productId);
    item.SetQuantity(quantity, nowUtc);
    Touch(nowUtc);
  }

  public void RemoveItem(Guid productId, DateTime nowUtc)
  {
    var item = FindItemOrThrow(productId);
    _items.Remove(item);
    Touch(nowUtc);
  }

  private CartItem FindItemOrThrow(Guid productId)
    => _items.FirstOrDefault(i => i.ProductId == productId)
      ?? throw new DomainException(ErrorCodes.NotFound, "El producto no esta en el carrito.");

  /// <summary>Cualquier actividad extiende el vencimiento — un carrito activo nunca expira solo.</summary>
  private void Touch(DateTime nowUtc)
  {
    UpdatedAtUtc = nowUtc;
    ExpiresAtUtc = nowUtc.Add(Ttl);
  }

  private static string NormalizeCartToken(string cartToken)
  {
    if (string.IsNullOrWhiteSpace(cartToken))
      throw new DomainException(ErrorCodes.ValidationError, "El CartToken es obligatorio.");

    return cartToken.Trim();
  }
}
