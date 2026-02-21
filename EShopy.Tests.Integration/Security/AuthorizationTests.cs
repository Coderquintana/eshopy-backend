using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Integration.Security;

public sealed class AuthorizationTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public AuthorizationTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task GetProducts_WithoutToken_Returns401()
  {
    var client = _factory.CreateClient();

    var response = await client.GetAsync("/api/products");

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task GetProducts_WithCatalogReadPermission_Returns200()
  {
    var client = _factory.CreateClient();
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["catalog.read"],
      roles: ["TENANT_STAFF"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await client.GetAsync("/api/products");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact]
  public async Task CreateProduct_WithoutCatalogWritePermission_Returns403()
  {
    var client = _factory.CreateClient();
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["catalog.read"],
      roles: ["TENANT_STAFF"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await client.PostAsJsonAsync("/api/products", new
    {
      slug = "from-auth-test",
      sku = "AUTH-TEST-001",
      name = "From Auth Test",
      description = "Auth permissions test",
      price = 12.50m,
      stockOnHand = 7
    });

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }
}
