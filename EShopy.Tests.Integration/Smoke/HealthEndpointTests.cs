using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;

  public HealthEndpointTests(WebApplicationFactory<Program> factory)
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
