using EShopy.Application.Carts;
using EShopy.Application.Common.Context;
using EShopy.Application.Common.Payments;
using EShopy.Application.Common.Stores;
using EShopy.Application.Orders.Contracts;
using EShopy.Application.Products;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Orders;
using EShopy.Domain.Payments;
using EShopy.Domain.Products;

namespace EShopy.Application.Orders.Commands;

public sealed class CheckoutCommandHandler(
  ICartRepository cartRepository,
  IProductRepository productRepository,
  IStoreService storeService,
  IPaymentProviderAdapter paymentProviderAdapter,
  ICheckoutWriter checkoutWriter,
  TenantContext tenantContext)
{
  private readonly CheckoutCommandValidator _validator = new();

  public async Task<Result<CheckoutResultDto>> Handle(CheckoutCommand command, string cartToken, CancellationToken ct)
  {
    var validation = _validator.Validate(command);
    if (!validation.IsValid)
    {
      var msg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      return Result<CheckoutResultDto>.Fail(ErrorCodes.ValidationError, msg);
    }

    if (string.IsNullOrWhiteSpace(cartToken))
      return Result<CheckoutResultDto>.Fail(ErrorCodes.ValidationError, "El header X-Cart-Token es obligatorio.");

    if (!tenantContext.TenantId.HasValue)
      return Result<CheckoutResultDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;

    var cart = await cartRepository.GetByCartTokenAsync(tenantId, cartToken, ct);
    if (cart is null || cart.Items.Count == 0)
      return Result<CheckoutResultDto>.Fail(ErrorCodes.ValidationError, "El carrito esta vacio.");

    var store = await storeService.GetDefaultStoreAsync(tenantId, ct);
    if (store is null)
      return Result<CheckoutResultDto>.Fail(ErrorCodes.NotFound, "No existe un store configurado para este tenant.");

    // 1. Validar productos y armar el snapshot ANTES de tocar el provider o la DB.
    var productIds = cart.Items.Select(i => i.ProductId).ToList();
    var products = await productRepository.GetByIdsAsync(tenantId, productIds, ct);
    var productsById = products.ToDictionary(p => p.Id);

    var orderItemsData = new List<OrderItemData>();
    foreach (var cartItem in cart.Items)
    {
      if (!productsById.TryGetValue(cartItem.ProductId, out var product) || product.Status != ProductStatus.Active)
        return Result<CheckoutResultDto>.Fail(ErrorCodes.ProductNotAvailable, $"El producto '{cartItem.ProductId}' ya no esta disponible.");

      orderItemsData.Add(new OrderItemData(product.Id, product.Name, product.Sku, product.Price, cartItem.Quantity));
    }

    try
    {
      var now = DateTime.UtcNow;
      var order = Order.Create(tenantId, store.Id, command.BuyerEmail, command.BuyerName, command.ShippingAddress,
        cartToken, orderItemsData, store.CurrencyCode, now);

      // 2. Iniciar el pago ANTES de escribir localmente (mismo principio que Keycloak en el
      //    onboarding: si el provider falla, no queda nada local que limpiar).
      var initiateResult = await paymentProviderAdapter.InitiateAsync(
        new InitiatePaymentRequest(order.Id, order.TotalAmount, store.CurrencyCode), ct);

      var payment = Payment.CreateInitiated(tenantId, order.Id, paymentProviderAdapter.Provider,
        order.TotalAmount, store.CurrencyCode, initiateResult.ProviderPaymentId, initiateResult.PaymentUrl, now);

      order.AttachPayment(payment.Id, now);

      // 3. Escritura local atomica: Order + OrderItems + Payment + OrderNumber juntos.
      var orderNumber = await checkoutWriter.CreateAsync(order, payment, ct);

      // 4. El carrito ya se convirtio en Order — no es parte de la transaccion atomica (best-effort:
      //    si esto falla, queda un carrito vacio-de-facto sin usar, no rompe nada del pedido ya creado).
      await cartRepository.DeleteAsync(cart, ct);

      return Result<CheckoutResultDto>.Ok(new CheckoutResultDto
      {
        OrderId = order.Id,
        OrderNumber = orderNumber,
        TotalAmount = order.TotalAmount,
        CurrencyCode = store.CurrencyCode,
        PaymentUrl = payment.ProviderPaymentUrl!
      });
    }
    catch (DomainException ex)
    {
      return Result<CheckoutResultDto>.Fail(ex.Code, ex.Message);
    }
  }
}
