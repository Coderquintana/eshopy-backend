using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class HealthEndpointTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public HealthEndpointTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Health_ShouldReturnOk()
  {
    var client = _factory.CreateClient();
    var res = await client.GetAsync("/health");
    res.IsSuccessStatusCode.Should().BeTrue();
  }
}
