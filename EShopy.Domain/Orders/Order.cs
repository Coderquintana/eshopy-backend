using EShopy.Domain.Common.Entities;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Domain.Orders;

/// <summary>
/// Pedido generado desde checkout. Agregado raiz de <see cref="OrderItem"/> (coleccion encapsulada,
/// mismo patron que Cart). OrderNumber no se conoce al crear — lo asigna ICheckoutWriter de forma
/// atomica recien al persistir.
/// </summary>
public sealed class Order : AppEntity
{
  private readonly List<OrderItem> _items = [];

  private Order(Guid id,
    Guid tenantId,
    Guid storeId,
    string buyerEmail,
    string buyerName,
    string? shippingAddress,
    string cartToken,
    string currencyCode,
    decimal totalAmount,
    DateTime createdAtUtc)
    : base(id, tenantId, createdAtUtc, createdBy: null, createdAtUtc, updatedBy: null, data: null)
  {
    StoreId = storeId;
    OrderNumber = 0;
    Status = OrderStatus.PendingPayment;
    BuyerEmail = buyerEmail;
    BuyerName = buyerName;
    ShippingAddress = shippingAddress;
    CartToken = cartToken;
    CurrencyCode = currencyCode;
    TotalAmount = totalAmount;
    PaymentId = null;
  }

  public Guid StoreId { get; private set; }

  /// <summary>0 hasta que ICheckoutWriter lo asigna atomicamente via AssignOrderNumber.</summary>
  public int OrderNumber { get; private set; }
  public OrderStatus Status { get; private set; }
  public string BuyerEmail { get; private set; }
  public string BuyerName { get; private set; }
  public string? ShippingAddress { get; private set; }
  public string CartToken { get; private set; }
  public string CurrencyCode { get; private set; }
  public decimal TotalAmount { get; private set; }
  public Guid? PaymentId { get; private set; }
  public IReadOnlyList<OrderItem> Items => _items;

  public static Order Create(
    Guid tenantId,
    Guid storeId,
    string buyerEmail,
    string buyerName,
    string? shippingAddress,
    string cartToken,
    IReadOnlyList<OrderItemData> items,
    string currencyCode,
    DateTime createdAtUtc)
  {
    var normalizedEmail = EnsureBuyerEmail(buyerEmail);
    var normalizedName = EnsureBuyerName(buyerName);

    if (items.Count == 0)
      throw new DomainException(ErrorCodes.ValidationError, "El pedido debe tener al menos un item.");

    var id = Guid.NewGuid();
    var orderItems = items.Select(data => OrderItem.Create(id, data)).ToList();
    var totalAmount = orderItems.Sum(i => i.Subtotal);

    var order = new Order(id, tenantId, storeId, normalizedEmail, normalizedName,
      NormalizeOptional(shippingAddress), cartToken, currencyCode, totalAmount, createdAtUtc);

    order._items.AddRange(orderItems);
    return order;
  }

  /// <summary>
  /// Llamado por ICheckoutWriter con el valor generado atomicamente. Idempotente a proposito: bajo
  /// contencion real el writer reintenta sobre la MISMA instancia de Order (ver EfCheckoutWriter),
  /// asi que puede llamarse mas de una vez antes de que la escritura efectivamente se confirme —
  /// solo el valor del intento que realmente persiste importa.
  /// </summary>
  public void AssignOrderNumber(int orderNumber)
  {
    OrderNumber = orderNumber;
  }

  public void AttachPayment(Guid paymentId, DateTime updatedAtUtc)
  {
    PaymentId = paymentId;
    UpdatedAtUtc = updatedAtUtc;
  }

  /// <remarks>
  /// Transiciones validas:
  /// PendingPayment → Paid | PendingPayment → Cancelled | Paid → Refunded
  /// </remarks>
  public void ChangeStatus(OrderStatus newStatus, DateTime updatedAtUtc)
  {
    if (Status == newStatus)
      return;

    var allowed = (Status, newStatus) switch
    {
      (OrderStatus.PendingPayment, OrderStatus.Paid) => true,
      (OrderStatus.PendingPayment, OrderStatus.Cancelled) => true,
      (OrderStatus.Paid, OrderStatus.Refunded) => true,
      _ => false
    };

    if (!allowed)
      throw new DomainException(ErrorCodes.OrderInvalidState, $"Transicion de estado no permitida: {Status} → {newStatus}.");

    Status = newStatus;
    UpdatedAtUtc = updatedAtUtc;
  }

  private static string EnsureBuyerEmail(string buyerEmail)
  {
    if (string.IsNullOrWhiteSpace(buyerEmail))
      throw new DomainException(ErrorCodes.ValidationError, "El email del comprador es obligatorio.");

    return buyerEmail.Trim();
  }

  private static string EnsureBuyerName(string buyerName)
  {
    if (string.IsNullOrWhiteSpace(buyerName))
      throw new DomainException(ErrorCodes.ValidationError, "El nombre del comprador es obligatorio.");

    return buyerName.Trim();
  }

  private static string? NormalizeOptional(string? value)
    => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
