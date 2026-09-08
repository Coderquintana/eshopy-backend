using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Application.Carts.Commands;
using EShopy.Application.Orders.Commands;
using EShopy.Application.Products.Commands;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;
using EShopy.Domain.Tenants;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EShopy.Tests.Integration.Security;

public sealed class TenantMembershipTests : IClassFixture<SecurityWebApplicationFactory>
{
  private const string OwnerAUserId = "owner-tenant-a";
  private readonly SecurityWebApplicationFactory _factory;
  private readonly InMemoryTenantsState _tenantsState;

  public TenantMembershipTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
    _tenantsState = factory.Services.GetRequiredService<InMemoryTenantsState>();

    EnsureActiveMembership(
      _tenantsState.TenantAId,
      OwnerAUserId,
      "owner-a@eshopy.local",
      TenantUserRole.Owner);
  }

  [Fact]
  public async Task TenantOwner_WithCatalogWrite_CannotCreateProductInAnotherTenant()
  {
    var client = CreateTenantBClient();
    SetToken(client, OwnerAUserId, "TENANT_OWNER", "catalog.write");

    var response = await client.PostAsJsonAsync(
      "/api/products",
      CreateProductCommand("owner-cross-tenant-product"));

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task TenantOwner_WithCatalogWrite_CanCreateProductInOwnTenant()
  {
    var client = _factory.CreateClient();
    SetToken(client, OwnerAUserId, "TENANT_OWNER", "catalog.write");

    var response = await client.PostAsJsonAsync(
      "/api/products",
      CreateProductCommand("owner-own-tenant-product"));

    response.StatusCode.Should().Be(HttpStatusCode.Created);
  }

  [Fact]
  public async Task SuperAdmin_CanCreateProductInAnyTenantWithoutMembership()
  {
    var client = CreateTenantBClient();
    SetToken(client, "superadmin-without-membership", "ESHOPY_SUPERADMIN", "catalog.write");

    var response = await client.PostAsJsonAsync(
      "/api/products",
      CreateProductCommand("superadmin-cross-tenant-product"));

    response.StatusCode.Should().Be(HttpStatusCode.Created);
  }

  [Theory]
  [InlineData("TENANT_ADMIN", TenantUserRole.Admin)]
  [InlineData("TENANT_STAFF", TenantUserRole.Staff)]
  public async Task TenantAdminAndStaff_CannotUsePermissionsInAnotherTenant(
    string role,
    TenantUserRole membershipRole)
  {
    var userId = $"{role.ToLowerInvariant()}-tenant-a";
    EnsureActiveMembership(_tenantsState.TenantAId, userId, $"{userId}@eshopy.local", membershipRole);
    var client = CreateTenantBClient();
    SetToken(client, userId, role, "catalog.write");

    var response = await client.PostAsJsonAsync(
      "/api/products",
      CreateProductCommand($"{role.ToLowerInvariant()}-cross-tenant-product"));

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task InactiveTenantUser_CannotCreateProductInOwnTenant()
  {
    const string userId = "inactive-owner-tenant-a";
    var tenantUser = TenantUser.Create(
      _tenantsState.TenantAId,
      userId,
      "inactive-owner@eshopy.local",
      "Inactive Owner",
      TenantUserRole.Owner,
      DateTime.UtcNow);
    tenantUser.Deactivate(DateTime.UtcNow);
    _tenantsState.TenantUsers.Add(tenantUser);

    var client = _factory.CreateClient();
    SetToken(client, userId, "TENANT_OWNER", "catalog.write");

    var response = await client.PostAsJsonAsync(
      "/api/products",
      CreateProductCommand("inactive-owner-product"));

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task AnonymousStoreRequest_IsNotAffectedByMembershipCheck()
  {
    var client = CreateTenantBClient();

    var response = await client.GetAsync("/api/store");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact]
  public async Task AnonymousCheckout_IsNotAffectedByMembershipCheck()
  {
    var client = CreateTenantBClient();
    SetToken(client, "checkout-superadmin", "ESHOPY_SUPERADMIN", "catalog.write");
    var product = await CreateActiveProductAsync(client);

    client.DefaultRequestHeaders.Authorization = null;
    client.DefaultRequestHeaders.Add("X-Cart-Token", Guid.NewGuid().ToString("N"));
    var addItemResponse = await client.PostAsJsonAsync(
      "/api/cart/items",
      new AddCartItemCommand(product.Id, 1));
    addItemResponse.EnsureSuccessStatusCode();

    var response = await client.PostAsJsonAsync(
      "/api/checkout",
      new CheckoutCommand("buyer@eshopy.local", "Buyer Name", null));

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  private HttpClient CreateTenantBClient()
    => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://tenant-b.eshopy.test")
    });

  private void EnsureActiveMembership(Guid tenantId, string userId, string email, TenantUserRole role)
  {
    if (_tenantsState.TenantUsers.Any(user => user.TenantId == tenantId && user.KeycloakUserId == userId))
      return;

    _tenantsState.TenantUsers.Add(TenantUser.Create(
      tenantId,
      userId,
      email,
      userId,
      role,
      DateTime.UtcNow));
  }

  private static void SetToken(HttpClient client, string subject, string role, params string[] permissions)
  {
    var token = TestJwtTokenFactory.CreateToken(
      permissions: permissions,
      roles: [role],
      subject: subject,
      email: $"{subject}@eshopy.local");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
  }

  private static CreateProductCommand CreateProductCommand(string slug)
    => new(slug, null, "Membership Test Product", null, 1000m, 10);

  private static async Task<ProductAdminDto> CreateActiveProductAsync(HttpClient client)
  {
    var createResponse = await client.PostAsJsonAsync(
      "/api/products",
      CreateProductCommand("anonymous-checkout-membership-product"));
    createResponse.EnsureSuccessStatusCode();
    var product = (await createResponse.Content.ReadFromJsonAsync<ProductAdminDto>())!;

    var statusResponse = await client.PatchAsync(
      $"/api/products/{product.Id}/status",
      JsonContent.Create(new ChangeProductStatusCommand(product.Id, ProductStatus.Active)));
    statusResponse.EnsureSuccessStatusCode();

    return product;
  }
}
