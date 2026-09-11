using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EShopy.Application.Tenants.Commands;
using EShopy.Application.Tenants.Contracts;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class StoreProfileFlowTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public StoreProfileFlowTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task UpdateStore_ShouldPersistBusinessIdentityAndThemeForPublicReading()
  {
    var client = _factory.CreateClient();
    var token = TestJwtTokenFactory.CreateToken(
      permissions: ["store.write", "store.read"],
      roles: ["TENANT_ADMIN"]);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var command = new UpdateStoreCommand(
      "Aramí",
      "America/Asuncion",
      "#A855F7",
      "https://cdn.example.com/arami-logo.png",
      "#FFF7FC",
      "Moda con identidad propia.",
      "+595 981 123456",
      "hola@arami.com.py",
      "https://instagram.com/arami",
      "https://facebook.com/arami",
      "Asunción, Paraguay",
      "Lun a sáb, 09:00 a 18:00",
      "Georgia",
      "large",
      "rounded",
      "spacious");

    var updateResponse = await client.PutAsJsonAsync("/api/store", command);

    updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    client.DefaultRequestHeaders.Authorization = null;
    var publicProfile = await client.GetFromJsonAsync<StoreProfileDto>("/api/store");

    publicProfile.Should().NotBeNull();
    publicProfile!.Name.Should().Be("Aramí");
    publicProfile.ContactWhatsapp.Should().Be("+595 981 123456");
    publicProfile.ContactEmail.Should().Be("hola@arami.com.py");
    publicProfile.InstagramUrl.Should().Be("https://instagram.com/arami");
    publicProfile.FacebookUrl.Should().Be("https://facebook.com/arami");
    publicProfile.Address.Should().Be("Asunción, Paraguay");
    publicProfile.BusinessHours.Should().Be("Lun a sáb, 09:00 a 18:00");
    publicProfile.FontFamily.Should().Be("Georgia");
    publicProfile.HeadingScale.Should().Be("large");
    publicProfile.BorderRadius.Should().Be("rounded");
    publicProfile.SpacingDensity.Should().Be("spacious");
  }
}
