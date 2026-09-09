using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class ProductsEndpointsTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public ProductsEndpointsTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task ProductsFlow_ShouldCreateAndExposePublicProduct()
  {
    var client = CreateAuthorizedClient();
    var createCommand = new CreateProductCommand(
      Slug: $"coffee-mug-{Guid.NewGuid():N}"[..24],
      Sku: null,
      Name: "Coffee Mug",
      Description: "Ceramic",
      Price: 12.5m,
      StockOnHand: 5);

    var createResponse = await client.PostAsJsonAsync("/api/products", createCommand);
    createResponse.IsSuccessStatusCode.Should().BeTrue();

    var created = await createResponse.Content.ReadFromJsonAsync<ProductAdminDto>();
    created.Should().NotBeNull();
    created!.RowVersion.Should().NotBeNullOrWhiteSpace();

    var statusCommand = new ChangeProductStatusCommand(created.Id, ProductStatus.Active, created.RowVersion);
    var statusResponse = await client.PatchAsync($"/api/products/{created.Id}/status", JsonContent.Create(statusCommand));
    statusResponse.IsSuccessStatusCode.Should().BeTrue();

    var publicResponse = await client.GetAsync("/api/public/products");
    publicResponse.IsSuccessStatusCode.Should().BeTrue();

    var paged = await publicResponse.Content.ReadFromJsonAsync<PagedResult<ProductPublicDto>>();
    paged.Should().NotBeNull();
    paged!.Items.Should().ContainSingle(item => item.Id == created.Id);
  }

  [Fact]
  public async Task UpdateProduct_WithStaleRowVersion_ShouldReturn409()
  {
    var client = CreateAuthorizedClient();
    var created = await CreateProductAsync(client);

    var firstUpdate = new UpdateProductCommand(
      created.Id, "First update", null, 15m, 5, null, created.RowVersion);
    var firstResponse = await client.PutAsJsonAsync($"/api/products/{created.Id}", firstUpdate);
    firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var updated = (await firstResponse.Content.ReadFromJsonAsync<ProductAdminDto>())!;
    updated.RowVersion.Should().NotBe(created.RowVersion);

    var staleUpdate = new UpdateProductCommand(
      created.Id, "Stale update", null, 20m, 5, null, created.RowVersion);
    var staleResponse = await client.PutAsJsonAsync($"/api/products/{created.Id}", staleUpdate);

    staleResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task ChangeStatus_WithStaleRowVersion_ShouldReturn409()
  {
    var client = CreateAuthorizedClient();
    var created = await CreateProductAsync(client);

    var activate = new ChangeProductStatusCommand(created.Id, ProductStatus.Active, created.RowVersion);
    var activateResponse = await client.PatchAsync(
      $"/api/products/{created.Id}/status",
      JsonContent.Create(activate));
    activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var staleArchive = new ChangeProductStatusCommand(created.Id, ProductStatus.Archived, created.RowVersion);
    var staleResponse = await client.PatchAsync(
      $"/api/products/{created.Id}/status",
      JsonContent.Create(staleArchive));

    staleResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task UpdateProduct_WithoutRowVersion_ShouldReturn400()
  {
    var client = CreateAuthorizedClient();
    var created = await CreateProductAsync(client);

    var response = await client.PutAsJsonAsync(
      $"/api/products/{created.Id}",
      new
      {
        name = "Missing version",
        description = (string?)null,
        price = 10m,
        stockOnHand = 5,
        sku = (string?)null
      });

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  private HttpClient CreateAuthorizedClient()
  {
    var client = _factory.CreateClient();
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["catalog.read", "catalog.write"],
      roles: ["TENANT_ADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
  }

  private static async Task<ProductAdminDto> CreateProductAsync(HttpClient client)
  {
    var command = new CreateProductCommand(
      $"concurrency-{Guid.NewGuid():N}"[..28],
      null,
      "Concurrency Product",
      null,
      10m,
      5);

    var response = await client.PostAsJsonAsync("/api/products", command);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<ProductAdminDto>())!;
  }
}
