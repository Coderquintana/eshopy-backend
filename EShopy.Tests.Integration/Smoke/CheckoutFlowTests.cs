using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Application.Carts.Commands;
using EShopy.Application.Carts.Contracts;
using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Orders.Commands;
using EShopy.Application.Orders.Contracts;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Orders;
using EShopy.Domain.Products;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class CheckoutFlowTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public CheckoutFlowTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task CheckoutFlow_ShouldCreateOrderAndBeVisibleToAdmin()
  {
    var client = _factory.CreateClient();
    var product = await CreateActiveProductAsync(client, "checkout-flow-product", price: 5000m);

    client.DefaultRequestHeaders.Authorization = null;
    client.DefaultRequestHeaders.Add("X-Cart-Token", Guid.NewGuid().ToString("N"));
    var addResponse = await client.PostAsJsonAsync("/api/cart/items", new AddCartItemCommand(product.Id, 3));
    addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var checkoutCommand = new CheckoutCommand("buyer@eshopy.local", "Buyer Name", "Calle Falsa 123");
    var checkoutResponse = await client.PostAsJsonAsync("/api/checkout", checkoutCommand);
    checkoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var checkoutResult = await checkoutResponse.Content.ReadFromJsonAsync<CheckoutResultDto>();
    checkoutResult.Should().NotBeNull();
    checkoutResult!.OrderNumber.Should().BeGreaterThan(0);
    checkoutResult.TotalAmount.Should().Be(15000m);
    checkoutResult.PaymentUrl.Should().NotBeNullOrWhiteSpace();

    // El carrito se vacio tras el checkout — un nuevo GetCart con el mismo token vuelve a estar vacio.
    var cartAfterCheckout = await client.GetAsync("/api/cart");
    var cartDto = await cartAfterCheckout.Content.ReadFromJsonAsync<CartDto>();
    cartDto!.Items.Should().BeEmpty();

    var adminToken = TestJwtTokenFactory.CreateToken(
      permissions: ["orders.read", "orders.write"],
      roles: ["TENANT_ADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

    var getByIdResponse = await client.GetAsync($"/api/orders/{checkoutResult.OrderId}");
    getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var orderDto = await getByIdResponse.Content.ReadFromJsonAsync<OrderAdminDto>();
    orderDto!.Status.Should().Be(nameof(OrderStatus.PendingPayment));
    orderDto.Items.Should().ContainSingle(i => i.ProductId == product.Id && i.Quantity == 3);

    var listResponse = await client.GetAsync("/api/orders");
    listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var paged = await listResponse.Content.ReadFromJsonAsync<PagedResult<OrderAdminDto>>();
    paged!.Items.Should().Contain(o => o.Id == checkoutResult.OrderId);

    var changeStatusCommand = new ChangeOrderStatusCommand(checkoutResult.OrderId, OrderStatus.Paid);
    var changeStatusResponse = await client.PatchAsync($"/api/orders/{checkoutResult.OrderId}/status", JsonContent.Create(changeStatusCommand));
    changeStatusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var updatedOrder = await changeStatusResponse.Content.ReadFromJsonAsync<OrderAdminDto>();
    updatedOrder!.Status.Should().Be(nameof(OrderStatus.Paid));
  }

  [Fact]
  public async Task Checkout_WithEmptyCart_ShouldReturn400()
  {
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Cart-Token", Guid.NewGuid().ToString("N"));

    var response = await client.PostAsJsonAsync("/api/checkout", new CheckoutCommand("buyer@eshopy.local", "Buyer Name", null));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Checkout_WithInvalidBuyerEmail_ShouldReturn400()
  {
    var client = _factory.CreateClient();
    var product = await CreateActiveProductAsync(client, "checkout-invalid-email-product", price: 1000m);

    client.DefaultRequestHeaders.Authorization = null;
    client.DefaultRequestHeaders.Add("X-Cart-Token", Guid.NewGuid().ToString("N"));
    await client.PostAsJsonAsync("/api/cart/items", new AddCartItemCommand(product.Id, 1));

    var response = await client.PostAsJsonAsync("/api/checkout", new CheckoutCommand("not-an-email", "Buyer Name", null));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task ChangeStatus_WithDisallowedTransition_ShouldReturn409()
  {
    var client = _factory.CreateClient();
    var product = await CreateActiveProductAsync(client, "checkout-invalid-transition-product", price: 1000m);

    client.DefaultRequestHeaders.Authorization = null;
    client.DefaultRequestHeaders.Add("X-Cart-Token", Guid.NewGuid().ToString("N"));
    await client.PostAsJsonAsync("/api/cart/items", new AddCartItemCommand(product.Id, 1));
    var checkoutResponse = await client.PostAsJsonAsync("/api/checkout", new CheckoutCommand("buyer@eshopy.local", "Buyer Name", null));
    var checkoutResult = (await checkoutResponse.Content.ReadFromJsonAsync<CheckoutResultDto>())!;

    var adminToken = TestJwtTokenFactory.CreateToken(
      permissions: ["orders.read", "orders.write"],
      roles: ["TENANT_ADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

    // PendingPayment → Refunded no es una transicion valida (solo Paid → Refunded).
    var response = await client.PatchAsync(
      $"/api/orders/{checkoutResult.OrderId}/status",
      JsonContent.Create(new ChangeOrderStatusCommand(checkoutResult.OrderId, OrderStatus.Refunded)));

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  private static async Task<ProductAdminDto> CreateActiveProductAsync(HttpClient client, string slug, decimal price)
  {
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["catalog.read", "catalog.write"],
      roles: ["TENANT_ADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var createCommand = new CreateProductCommand(slug, null, "Checkout Flow Product", null, price, 100);
    var createResponse = await client.PostAsJsonAsync("/api/products", createCommand);
    createResponse.EnsureSuccessStatusCode();
    var product = (await createResponse.Content.ReadFromJsonAsync<ProductAdminDto>())!;

    var statusCommand = new ChangeProductStatusCommand(product.Id, ProductStatus.Active);
    var statusResponse = await client.PatchAsync($"/api/products/{product.Id}/status", JsonContent.Create(statusCommand));
    statusResponse.EnsureSuccessStatusCode();

    return product;
  }
}
