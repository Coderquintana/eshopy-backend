using EShopy.Domain.Common.Results;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.SmokeTests;

public sealed class ResultTests
{
  [Fact]
  public void Ok_ShouldBeSuccess()
  {
    var r = Result.Ok();
    r.IsSuccess.Should().BeTrue();
  }

  [Fact]
  public void OkT_ShouldContainValue()
  {
    var r = Result<string>.Ok("x");
    r.IsSuccess.Should().BeTrue();
    r.Value.Should().Be("x");
  }
}
