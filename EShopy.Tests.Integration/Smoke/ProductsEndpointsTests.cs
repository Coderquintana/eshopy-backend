using System.Net.Http.Json;
using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class ProductsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;

  public ProductsEndpointsTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task ProductsFlow_ShouldCreateAndExposePublicProduct()
  {
    var client = _factory.CreateClient();

    var createCommand = new CreateProductCommand(
      Slug: "coffee-mug",
      Sku: null,
      Name: "Coffee Mug",
      Description: "Ceramic",
      Price: 12.5m,
      StockOnHand: 5);

    var createResponse = await client.PostAsJsonAsync("/api/products", createCommand);
    createResponse.IsSuccessStatusCode.Should().BeTrue();

    var created = await createResponse.Content.ReadFromJsonAsync<ProductAdminDto>();
    created.Should().NotBeNull();

    var statusCommand = new ChangeProductStatusCommand(created!.Id, ProductStatus.Active);
    var statusResponse = await client.PatchAsync($"/api/products/{created.Id}/status", JsonContent.Create(statusCommand));
    statusResponse.IsSuccessStatusCode.Should().BeTrue();

    var publicResponse = await client.GetAsync("/api/public/products");
    publicResponse.IsSuccessStatusCode.Should().BeTrue();

    var paged = await publicResponse.Content.ReadFromJsonAsync<PagedResult<ProductPublicDto>>();
    paged.Should().NotBeNull();
    paged!.Items.Should().ContainSingle(item => item.Slug == "coffee-mug");
  }
}
