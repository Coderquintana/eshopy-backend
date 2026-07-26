using EShopy.Application.Tenants.Commands;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Tenants;

public sealed class InviteTenantUserCommandValidatorTests
{
  [Fact]
  public void ShouldFailWhenEmailInvalid()
  {
    var validator = new InviteTenantUserCommandValidator();
    var command = new InviteTenantUserCommand("not-an-email", "Juan Perez", "admin");

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(InviteTenantUserCommand.Email));
  }

  [Fact]
  public void ShouldFailWhenNameEmpty()
  {
    var validator = new InviteTenantUserCommandValidator();
    var command = new InviteTenantUserCommand("user@example.com", "", "admin");

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(InviteTenantUserCommand.Name));
  }

  [Fact]
  public void ShouldFailWhenRoleIsOwner()
  {
    // Owner solo se crea una vez, durante el onboarding — no es invitable.
    var validator = new InviteTenantUserCommandValidator();
    var command = new InviteTenantUserCommand("user@example.com", "Juan Perez", "owner");

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(InviteTenantUserCommand.Role));
  }

  [Fact]
  public void ShouldFailWhenRoleUnknown()
  {
    var validator = new InviteTenantUserCommandValidator();
    var command = new InviteTenantUserCommand("user@example.com", "Juan Perez", "superadmin");

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(InviteTenantUserCommand.Role));
  }

  [Theory]
  [InlineData("admin")]
  [InlineData("staff")]
  [InlineData("ADMIN")]
  public void ShouldPassWithValidData(string role)
  {
    var validator = new InviteTenantUserCommandValidator();
    var command = new InviteTenantUserCommand("user@example.com", "Juan Perez", role);

    var result = validator.Validate(command);

    result.IsValid.Should().BeTrue();
  }
}
