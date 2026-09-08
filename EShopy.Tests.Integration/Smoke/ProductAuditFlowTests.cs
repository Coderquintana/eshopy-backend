using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Application.Common.Audit;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class ProductAuditFlowTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public ProductAuditFlowTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task ProductChanges_ShouldAuditPriceAndStatus()
  {
    var client = CreateAuthorizedClient();
    var created = await CreateProductAsync(client, 12.5m);

    var updateCommand = new UpdateProductCommand(
      created.Id,
      "Audited Product",
      "Updated description",
      15.75m,
      8,
      null);

    var updateResponse = await client.PutAsJsonAsync($"/api/products/{created.Id}", updateCommand);
    updateResponse.IsSuccessStatusCode.Should().BeTrue();

    var statusCommand = new ChangeProductStatusCommand(created.Id, ProductStatus.Active);
    var statusResponse = await client.PatchAsync(
      $"/api/products/{created.Id}/status",
      JsonContent.Create(statusCommand));
    statusResponse.IsSuccessStatusCode.Should().BeTrue();

    var auditLogger = (InMemoryAuditLogger)_factory.Services.GetRequiredService<IAuditLogger>();
    var entries = auditLogger.Entries.Where(entry => entry.EntityId == created.Id).ToList();

    entries.Should().ContainSingle(entry =>
      entry.Action == "Product.ChangePrice" &&
      entry.EntityType == "Product" &&
      entry.Details == "12.5 -> 15.75");

    entries.Should().ContainSingle(entry =>
      entry.Action == "Product.ChangeStatus" &&
      entry.EntityType == "Product" &&
      entry.Details == "Draft -> Active");
  }

  [Fact]
  public async Task UpdateProduct_WithoutPriceChange_ShouldNotAuditPrice()
  {
    var client = CreateAuthorizedClient();
    var created = await CreateProductAsync(client, 20m);

    var updateCommand = new UpdateProductCommand(
      created.Id,
      "Renamed Product",
      "Same price",
      20m,
      12,
      "SKU-AUDIT");

    var response = await client.PutAsJsonAsync($"/api/products/{created.Id}", updateCommand);
    response.IsSuccessStatusCode.Should().BeTrue();

    var auditLogger = (InMemoryAuditLogger)_factory.Services.GetRequiredService<IAuditLogger>();
    auditLogger.Entries.Should().NotContain(entry =>
      entry.EntityId == created.Id && entry.Action == "Product.ChangePrice");
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

  private static async Task<ProductAdminDto> CreateProductAsync(HttpClient client, decimal price)
  {
    var command = new CreateProductCommand(
      Slug: $"audit-{Guid.NewGuid():N}"[..20],
      Sku: null,
      Name: "Audit Product",
      Description: "Initial description",
      Price: price,
      StockOnHand: 10);

    var response = await client.PostAsJsonAsync("/api/products", command);
    response.IsSuccessStatusCode.Should().BeTrue();

    var created = await response.Content.ReadFromJsonAsync<ProductAdminDto>();
    created.Should().NotBeNull();
    return created!;
  }
}
