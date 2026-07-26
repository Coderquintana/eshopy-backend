using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using EShopy.Application.Carts.Commands;
using EShopy.Application.Orders.Commands;
using EShopy.Application.Orders.Contracts;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Orders;
using EShopy.Domain.Payments;
using EShopy.Domain.Products;
using EShopy.Infrastructure.Payments;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class PaymentWebhookFlowTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public PaymentWebhookFlowTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Webhook_WithCapturedEvent_ShouldMarkPaymentCapturedAndOrderPaid()
  {
    var client = _factory.CreateClient();
    var (orderId, providerPaymentId) = await CheckoutAsync(client, "webhook-captured-product");

    var response = await SendWebhookAsync(client, Guid.NewGuid().ToString(), providerPaymentId, "Captured");
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var order = await GetOrderAsync(client, orderId);
    order.Status.Should().Be(nameof(OrderStatus.Paid));
  }

  [Fact]
  public async Task Webhook_WithFailedEvent_ShouldMarkOrderCancelled()
  {
    var client = _factory.CreateClient();
    var (orderId, providerPaymentId) = await CheckoutAsync(client, "webhook-failed-product");

    var response = await SendWebhookAsync(client, Guid.NewGuid().ToString(), providerPaymentId, "Failed");
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var order = await GetOrderAsync(client, orderId);
    order.Status.Should().Be(nameof(OrderStatus.Cancelled));
  }

  [Fact]
  public async Task Webhook_WithDuplicateEventId_ShouldBeIdempotent()
  {
    var client = _factory.CreateClient();
    var (orderId, providerPaymentId) = await CheckoutAsync(client, "webhook-idempotent-product");
    var eventId = Guid.NewGuid().ToString();

    var first = await SendWebhookAsync(client, eventId, providerPaymentId, "Captured");
    first.StatusCode.Should().Be(HttpStatusCode.OK);

    // Reenvio del mismo EventId (igual que un provider reintentando) — no debe fallar ni reaplicar.
    var second = await SendWebhookAsync(client, eventId, providerPaymentId, "Captured");
    second.StatusCode.Should().Be(HttpStatusCode.OK);

    var order = await GetOrderAsync(client, orderId);
    order.Status.Should().Be(nameof(OrderStatus.Paid));
  }

  [Fact]
  public async Task Webhook_WithInvalidSignature_ShouldReturn401()
  {
    var client = _factory.CreateClient();
    var (_, providerPaymentId) = await CheckoutAsync(client, "webhook-invalid-signature-product");

    var body = BuildPayload(Guid.NewGuid().ToString(), providerPaymentId, "Captured");
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/fake")
    {
      Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    request.Headers.Add(FakePaymentProviderAdapter.WebhookSignatureHeader, "wrong-secret");

    var response = await client.SendAsync(request);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Webhook_WithUnknownProviderPaymentId_ShouldReturn404()
  {
    var client = _factory.CreateClient();

    var response = await SendWebhookAsync(client, Guid.NewGuid().ToString(), "unknown-provider-payment-id", "Captured");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  private static async Task<(Guid OrderId, string ProviderPaymentId)> CheckoutAsync(HttpClient client, string slug)
  {
    var product = await CreateActiveProductAsync(client, slug);

    client.DefaultRequestHeaders.Authorization = null;
    client.DefaultRequestHeaders.Add("X-Cart-Token", Guid.NewGuid().ToString("N"));
    await client.PostAsJsonAsync("/api/cart/items", new AddCartItemCommand(product.Id, 1));

    var checkoutResponse = await client.PostAsJsonAsync("/api/checkout", new CheckoutCommand("buyer@eshopy.local", "Buyer Name", null));
    var checkoutResult = (await checkoutResponse.Content.ReadFromJsonAsync<CheckoutResultDto>())!;

    // FakePaymentProviderAdapter arma la url como https://fake-payment.local/pay/{providerPaymentId}.
    var providerPaymentId = checkoutResult.PaymentUrl.Split('/').Last();

    client.DefaultRequestHeaders.Remove("X-Cart-Token");
    return (checkoutResult.OrderId, providerPaymentId);
  }

  private static async Task<OrderAdminDto> GetOrderAsync(HttpClient client, Guid orderId)
  {
    var adminToken = TestJwtTokenFactory.CreateToken(
      permissions: ["orders.read"],
      roles: ["TENANT_ADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

    var response = await client.GetAsync($"/api/orders/{orderId}");
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<OrderAdminDto>())!;
  }

  private static async Task<HttpResponseMessage> SendWebhookAsync(HttpClient client, string eventId, string providerPaymentId, string eventType)
  {
    var body = BuildPayload(eventId, providerPaymentId, eventType);
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/fake")
    {
      Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    request.Headers.Add(FakePaymentProviderAdapter.WebhookSignatureHeader, FakePaymentProviderAdapter.WebhookSecret);

    return await client.SendAsync(request);
  }

  private static string BuildPayload(string eventId, string providerPaymentId, string eventType)
    => $$"""{"eventId":"{{eventId}}","providerPaymentId":"{{providerPaymentId}}","eventType":"{{eventType}}"}""";

  private static async Task<ProductAdminDto> CreateActiveProductAsync(HttpClient client, string slug)
  {
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["catalog.read", "catalog.write"],
      roles: ["TENANT_ADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var createCommand = new CreateProductCommand(slug, null, "Webhook Flow Product", null, 1000m, 100);
    var createResponse = await client.PostAsJsonAsync("/api/products", createCommand);
    createResponse.EnsureSuccessStatusCode();
    var product = (await createResponse.Content.ReadFromJsonAsync<ProductAdminDto>())!;

    var statusCommand = new ChangeProductStatusCommand(product.Id, ProductStatus.Active);
    var statusResponse = await client.PatchAsync($"/api/products/{product.Id}/status", JsonContent.Create(statusCommand));
    statusResponse.EnsureSuccessStatusCode();

    return product;
  }
}
