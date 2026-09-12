using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Application.Carts.Commands;
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

/// <summary>
/// F8-07: consulta publica del pedido tras el checkout, autorizada por el AccessToken en vez de por
/// sesion. Es lo que alimenta la pantalla de confirmacion de compra del storefront.
/// </summary>
public sealed class PublicOrderFlowTests : IClassFixture<SecurityWebApplicationFactory>
{
  private const string OrderTokenHeader = "X-Order-Token";

  private readonly SecurityWebApplicationFactory _factory;

  public PublicOrderFlowTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task GetPublicOrder_WithTheCheckoutAccessToken_ShouldReturnTheReducedView()
  {
    var client = _factory.CreateClient();
    var (checkoutResult, productId) = await CheckoutAsync(client, "public-order-happy", price: 5000m, quantity: 3);

    checkoutResult.AccessToken.Should().NotBeNullOrWhiteSpace();

    // Cliente nuevo, sin token de admin ni X-Cart-Token: simula la navegacion que vuelve del provider.
    var buyer = _factory.CreateClient();
    buyer.DefaultRequestHeaders.Add(OrderTokenHeader, checkoutResult.AccessToken);

    var response = await buyer.GetAsync($"/api/public/orders/{checkoutResult.OrderId}");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var dto = (await response.Content.ReadFromJsonAsync<PublicOrderDto>())!;
    dto.Id.Should().Be(checkoutResult.OrderId);
    dto.OrderNumber.Should().Be(checkoutResult.OrderNumber);
    dto.Status.Should().Be(nameof(OrderStatus.PendingPayment));
    dto.TotalAmount.Should().Be(15000m);
    dto.CurrencyCode.Should().Be(checkoutResult.CurrencyCode);
    dto.Items.Should().ContainSingle(i => i.ProductId == productId && i.Quantity == 3);
  }

  [Fact]
  public async Task GetPublicOrder_ShouldNotLeakBuyerPersonalData()
  {
    var client = _factory.CreateClient();
    var (checkoutResult, _) = await CheckoutAsync(client, "public-order-no-pii", price: 1000m, quantity: 1);

    var buyer = _factory.CreateClient();
    buyer.DefaultRequestHeaders.Add(OrderTokenHeader, checkoutResult.AccessToken);

    var response = await buyer.GetAsync($"/api/public/orders/{checkoutResult.OrderId}");

    var body = await response.Content.ReadAsStringAsync();
    body.Should().NotContain(BuyerEmail);
    body.Should().NotContain(BuyerName);
    body.Should().NotContain(ShippingAddress);
    // El AccessToken tampoco vuelve en la lectura: solo se entrega una vez, en el checkout.
    body.Should().NotContain(checkoutResult.AccessToken);
  }

  [Fact]
  public async Task GetPublicOrder_WithAWrongAccessToken_ShouldReturn404()
  {
    var client = _factory.CreateClient();
    var (checkoutResult, _) = await CheckoutAsync(client, "public-order-wrong-token", price: 1000m, quantity: 1);

    var buyer = _factory.CreateClient();
    buyer.DefaultRequestHeaders.Add(OrderTokenHeader, "no-es-el-token-del-pedido");

    var response = await buyer.GetAsync($"/api/public/orders/{checkoutResult.OrderId}");

    // 404 y no 403: distinguirlos permitiria enumerar que pedidos existen en el tenant.
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task GetPublicOrder_WithAnotherOrdersAccessToken_ShouldReturn404()
  {
    var client = _factory.CreateClient();
    var (first, _) = await CheckoutAsync(client, "public-order-cross-a", price: 1000m, quantity: 1);
    var (second, _) = await CheckoutAsync(_factory.CreateClient(), "public-order-cross-b", price: 1000m, quantity: 1);

    var buyer = _factory.CreateClient();
    buyer.DefaultRequestHeaders.Add(OrderTokenHeader, second.AccessToken);

    var response = await buyer.GetAsync($"/api/public/orders/{first.OrderId}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task GetPublicOrder_WithoutTheHeader_ShouldReturn400()
  {
    var client = _factory.CreateClient();
    var (checkoutResult, _) = await CheckoutAsync(client, "public-order-no-header", price: 1000m, quantity: 1);

    var buyer = _factory.CreateClient();

    var response = await buyer.GetAsync($"/api/public/orders/{checkoutResult.OrderId}");

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task GetPublicOrder_WithAnUnknownOrderId_ShouldReturn404()
  {
    var buyer = _factory.CreateClient();
    buyer.DefaultRequestHeaders.Add(OrderTokenHeader, "cualquier-token");

    var response = await buyer.GetAsync($"/api/public/orders/{Guid.NewGuid()}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  private const string BuyerEmail = "public-order-buyer@eshopy.local";
  private const string BuyerName = "Comprador Anonimo";
  private const string ShippingAddress = "Avenida Siempre Viva 742";

  private static async Task<(CheckoutResultDto Result, Guid ProductId)> CheckoutAsync(
    HttpClient client, string slug, decimal price, int quantity)
  {
    var product = await CreateActiveProductAsync(client, slug, price);

    client.DefaultRequestHeaders.Authorization = null;
    client.DefaultRequestHeaders.Remove("X-Cart-Token");
    client.DefaultRequestHeaders.Add("X-Cart-Token", Guid.NewGuid().ToString("N"));

    var addResponse = await client.PostAsJsonAsync("/api/cart/items", new AddCartItemCommand(product.Id, quantity));
    addResponse.EnsureSuccessStatusCode();

    var checkoutResponse = await client.PostAsJsonAsync("/api/checkout",
      new CheckoutCommand(BuyerEmail, BuyerName, ShippingAddress));
    checkoutResponse.EnsureSuccessStatusCode();

    return ((await checkoutResponse.Content.ReadFromJsonAsync<CheckoutResultDto>())!, product.Id);
  }

  private static async Task<ProductAdminDto> CreateActiveProductAsync(HttpClient client, string slug, decimal price)
  {
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["catalog.read", "catalog.write"],
      roles: ["TENANT_ADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var createCommand = new CreateProductCommand(slug, null, "Public Order Product", null, price, 100);
    var createResponse = await client.PostAsJsonAsync("/api/products", new[] { createCommand });
    createResponse.EnsureSuccessStatusCode();
    var product = (await createResponse.Content.ReadFromJsonAsync<IReadOnlyList<ProductAdminDto>>())!.Single();

    var statusCommand = new ChangeProductStatusCommand(product.Id, ProductStatus.Active, product.RowVersion);
    var statusResponse = await client.PatchAsync($"/api/products/{product.Id}/status", JsonContent.Create(statusCommand));
    statusResponse.EnsureSuccessStatusCode();

    return (await statusResponse.Content.ReadFromJsonAsync<ProductAdminDto>())!;
  }
}
