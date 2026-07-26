using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Application.Carts.Commands;
using EShopy.Application.Carts.Contracts;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class CartFlowTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public CartFlowTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task CartFlow_ShouldAddAccumulateUpdateAndRemoveItems()
  {
    var client = _factory.CreateClient();
    var product = await CreateActiveProductAsync(client, "cart-flow-product");

    client.DefaultRequestHeaders.Authorization = null; // Cart es anonimo
    client.DefaultRequestHeaders.Add("X-Cart-Token", Guid.NewGuid().ToString("N"));

    var emptyResponse = await client.GetAsync("/api/cart");
    emptyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var emptyCart = await emptyResponse.Content.ReadFromJsonAsync<CartDto>();
    emptyCart!.Items.Should().BeEmpty();

    var addResponse = await client.PostAsJsonAsync("/api/cart/items", new AddCartItemCommand(product.Id, 2));
    addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var cartAfterAdd = await addResponse.Content.ReadFromJsonAsync<CartDto>();
    cartAfterAdd!.Items.Should().ContainSingle(i => i.ProductId == product.Id && i.Quantity == 2);

    // Agregar el mismo producto de nuevo acumula, no duplica.
    var secondAddResponse = await client.PostAsJsonAsync("/api/cart/items", new AddCartItemCommand(product.Id, 3));
    var cartAfterSecondAdd = await secondAddResponse.Content.ReadFromJsonAsync<CartDto>();
    cartAfterSecondAdd!.Items.Should().ContainSingle(i => i.Quantity == 5);

    var updateResponse = await client.PutAsJsonAsync(
      $"/api/cart/items/{product.Id}", new UpdateCartItemQuantityCommand(product.Id, 10));
    updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var cartAfterUpdate = await updateResponse.Content.ReadFromJsonAsync<CartDto>();
    cartAfterUpdate!.Items[0].Quantity.Should().Be(10);
    cartAfterUpdate.Subtotal.Should().Be(cartAfterUpdate.Items[0].UnitPrice * 10);

    var removeResponse = await client.DeleteAsync($"/api/cart/items/{product.Id}");
    removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var cartAfterRemove = await removeResponse.Content.ReadFromJsonAsync<CartDto>();
    cartAfterRemove!.Items.Should().BeEmpty();
  }

  [Fact]
  public async Task AddItem_WithProductThatDoesNotExist_ShouldReturn409()
  {
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Cart-Token", Guid.NewGuid().ToString("N"));

    var response = await client.PostAsJsonAsync("/api/cart/items", new AddCartItemCommand(Guid.NewGuid(), 1));

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task UpdateItemQuantity_OnEmptyCart_ShouldReturn404()
  {
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Cart-Token", Guid.NewGuid().ToString("N"));

    var response = await client.PutAsJsonAsync(
      $"/api/cart/items/{Guid.NewGuid()}", new UpdateCartItemQuantityCommand(Guid.NewGuid(), 5));

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  private static async Task<ProductAdminDto> CreateActiveProductAsync(HttpClient client, string slug)
  {
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["catalog.read", "catalog.write"],
      roles: ["TENANT_ADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var createCommand = new CreateProductCommand(slug, null, "Cart Flow Product", null, 5000m, 100);
    var createResponse = await client.PostAsJsonAsync("/api/products", createCommand);
    createResponse.EnsureSuccessStatusCode();
    var product = (await createResponse.Content.ReadFromJsonAsync<ProductAdminDto>())!;

    var statusCommand = new ChangeProductStatusCommand(product.Id, ProductStatus.Active);
    var statusResponse = await client.PatchAsync($"/api/products/{product.Id}/status", JsonContent.Create(statusCommand));
    statusResponse.EnsureSuccessStatusCode();

    return product;
  }
}
