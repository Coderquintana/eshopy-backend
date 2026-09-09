using System.Net;
using System.Net.Http.Json;
using EShopy.Api.Controllers.Public;
using EShopy.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EShopy.Tests.Integration.Smoke;

public sealed class ClientErrorsEndpointTests : IClassFixture<SecurityWebApplicationFactory>
{
  private readonly SecurityWebApplicationFactory _factory;

  public ClientErrorsEndpointTests(SecurityWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Report_ShouldAcceptAnonymousRequestWithoutTenantSubdomain()
  {
    var client = _factory.CreateClient();

    var response = await client.PostAsJsonAsync("/api/client-errors", new ClientErrorReport(
      "Unexpected frontend error",
      "Error: Unexpected frontend error\n    at app.ts:10:4",
      "http://localhost/login",
      "eShopy integration test"));

    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
  }

  [Fact]
  public void Report_ShouldTruncatePublicPayloadBeforeLogging()
  {
    var logger = new CapturingLogger<ClientErrorsController>();
    var controller = new ClientErrorsController(logger);

    var result = controller.Report(new ClientErrorReport(
      new string('m', 2500),
      new string('s', 3000),
      new string('u', 3000),
      new string('a', 1000)));

    result.Should().BeOfType<NoContentResult>();
    logger.Properties["ClientMessage"].Should().BeOfType<string>().Which.Should().HaveLength(2000);
    logger.Properties["ClientStack"].Should().BeOfType<string>().Which.Should().HaveLength(2000);
    logger.Properties["ClientUrl"].Should().BeOfType<string>().Which.Should().HaveLength(2048);
    logger.Properties["ClientUserAgent"].Should().BeOfType<string>().Which.Should().HaveLength(512);
  }

  [Fact]
  public void Report_ShouldRemainBestEffortWhenLoggingFails()
  {
    var controller = new ClientErrorsController(new ThrowingLogger<ClientErrorsController>());
    IActionResult? result = null;

    Action action = () => result = controller.Report(new ClientErrorReport("Frontend error"));

    action.Should().NotThrow();
    result.Should().BeOfType<NoContentResult>();
  }

  private sealed class CapturingLogger<T> : ILogger<T>
  {
    public Dictionary<string, object?> Properties { get; } = new(StringComparer.Ordinal);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      if (state is not IEnumerable<KeyValuePair<string, object?>> properties)
        return;

      foreach (var property in properties)
      {
        if (property.Key != "{OriginalFormat}")
          Properties[property.Key] = property.Value;
      }
    }
  }

  private sealed class ThrowingLogger<T> : ILogger<T>
  {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter) =>
      throw new InvalidOperationException("Simulated logging failure.");
  }
}
