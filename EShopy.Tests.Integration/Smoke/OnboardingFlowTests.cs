using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Application.Tenants.Commands;
using EShopy.Application.Tenants.Contracts;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class OnboardingFlowTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public OnboardingFlowTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task OnboardingFlow_ShouldCreateAndActivateTenant()
  {
    var client = _factory.CreateClient();

    var createCommand = new CreateTenantCommand(
      Subdomain: $"onboarding-{Guid.NewGuid():N}"[..20],
      BusinessName: "Mi Tienda SRL",
      OwnerEmail: "owner@mitienda.com",
      OwnerName: "Juan Perez",
      Plan: "basic",
      CurrencyCode: "usd");

    var createResponse = await client.PostAsJsonAsync("/api/onboarding/tenants", createCommand);
    createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

    var created = await createResponse.Content.ReadFromJsonAsync<TenantOnboardingResultDto>();
    created.Should().NotBeNull();
    created!.Status.Should().Be("PendingPayment");
    created.OwnerTemporaryPassword.Should().NotBeNullOrWhiteSpace();

    var superadminToken = TestJwtTokenFactory.CreateToken(
      permissions: ["tenants.write", "tenants.read"],
      roles: ["ESHOPY_SUPERADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", superadminToken);

    var activateResponse = await client.PostAsync($"/api/admin/tenants/{created.TenantId}/activate", content: null);
    activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var activated = await activateResponse.Content.ReadFromJsonAsync<TenantAdminDto>();
    activated!.Status.Should().Be("Active");

    var getResponse = await client.GetAsync($"/api/admin/tenants/{created.TenantId}");
    getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var fetched = await getResponse.Content.ReadFromJsonAsync<TenantAdminDto>();
    fetched!.Status.Should().Be("Active");
  }

  [Fact]
  public async Task CreateTenant_WithDuplicateSubdomain_ShouldReturn409()
  {
    var client = _factory.CreateClient();
    var subdomain = $"dup-{Guid.NewGuid():N}"[..15];
    var command = new CreateTenantCommand(subdomain, "Mi Tienda SRL", "owner@mitienda.com", "Juan Perez", "basic", "PYG");

    var first = await client.PostAsJsonAsync("/api/onboarding/tenants", command);
    first.StatusCode.Should().Be(HttpStatusCode.Created);

    var second = await client.PostAsJsonAsync("/api/onboarding/tenants", command with { OwnerEmail = "otro@mitienda.com" });

    second.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task CreateTenant_WithInvalidCurrencyCode_ShouldReturn400()
  {
    var client = _factory.CreateClient();
    var command = new CreateTenantCommand(
      $"currency-{Guid.NewGuid():N}"[..18],
      "Mi Tienda SRL",
      "owner@mitienda.com",
      "Juan Perez",
      "basic",
      "US1");

    var response = await client.PostAsJsonAsync("/api/onboarding/tenants", command);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }
}
