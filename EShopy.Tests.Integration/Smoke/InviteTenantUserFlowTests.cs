using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Application.Tenants.Commands;
using EShopy.Application.Tenants.Contracts;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class InviteTenantUserFlowTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public InviteTenantUserFlowTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task InviteFlow_ShouldCreateAndListTenantUser()
  {
    var client = _factory.CreateClient();
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["users.manage"],
      roles: ["TENANT_OWNER"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var inviteCommand = new InviteTenantUserCommand("staff@mitienda.com", "Staff Member", "staff");
    var inviteResponse = await client.PostAsJsonAsync("/api/admin/users", inviteCommand);
    inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

    var invited = await inviteResponse.Content.ReadFromJsonAsync<TenantUserDto>();
    invited.Should().NotBeNull();
    invited!.Email.Should().Be("staff@mitienda.com");
    invited.Role.Should().Be("Staff");
    invited.IsActive.Should().BeTrue();

    var listResponse = await client.GetAsync("/api/admin/users");
    listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var users = await listResponse.Content.ReadFromJsonAsync<List<TenantUserDto>>();
    users.Should().ContainSingle(u => u.Email == "staff@mitienda.com");
  }

  [Fact]
  public async Task Invite_WithoutUsersManagePermission_ShouldReturn403()
  {
    var client = _factory.CreateClient();
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["catalog.read"],
      roles: ["TENANT_STAFF"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await client.PostAsJsonAsync("/api/admin/users",
      new InviteTenantUserCommand("nope@mitienda.com", "Nope", "staff"));

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task Invite_WithDuplicateEmailInSameTenant_ShouldReturn409()
  {
    var client = _factory.CreateClient();
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["users.manage"],
      roles: ["TENANT_OWNER"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var command = new InviteTenantUserCommand("duplicado@mitienda.com", "Duplicado", "admin");

    var first = await client.PostAsJsonAsync("/api/admin/users", command);
    first.StatusCode.Should().Be(HttpStatusCode.Created);

    var second = await client.PostAsJsonAsync("/api/admin/users", command);
    second.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }
}
