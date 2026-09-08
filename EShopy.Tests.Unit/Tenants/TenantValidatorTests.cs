using EShopy.Application.Tenants.Commands;
using FluentAssertions;
using Xunit;

namespace EShopy.Tests.Unit.Tenants;

public sealed class TenantValidatorTests
{
  // ─── CreateTenantCommandValidator ────────────────────────────────────────

  [Fact]
  public void CreateValidator_ShouldFailWhenSubdomainTooShort()
  {
    var validator = new CreateTenantCommandValidator();
    var command = new CreateTenantCommand("ab", "Mi Tienda SRL", "owner@mitienda.com", "Juan Perez", "basic", "PYG");

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTenantCommand.Subdomain));
  }

  [Fact]
  public void CreateValidator_ShouldFailWhenSubdomainHasInvalidChars()
  {
    var validator = new CreateTenantCommandValidator();
    var command = new CreateTenantCommand("mi tienda!", "Mi Tienda SRL", "owner@mitienda.com", "Juan Perez", "basic", "PYG");

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTenantCommand.Subdomain));
  }

  [Fact]
  public void CreateValidator_ShouldFailWhenOwnerEmailInvalid()
  {
    var validator = new CreateTenantCommandValidator();
    var command = new CreateTenantCommand("mitienda", "Mi Tienda SRL", "not-an-email", "Juan Perez", "basic", "PYG");

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTenantCommand.OwnerEmail));
  }

  [Fact]
  public void CreateValidator_ShouldFailWhenPlanUnknown()
  {
    var validator = new CreateTenantCommandValidator();
    var command = new CreateTenantCommand("mitienda", "Mi Tienda SRL", "owner@mitienda.com", "Juan Perez", "enterprise", "PYG");

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTenantCommand.Plan));
  }

  [Theory]
  [InlineData("")]
  [InlineData("US")]
  [InlineData("US1")]
  [InlineData("EURO")]
  public void CreateValidator_ShouldFailWhenCurrencyCodeInvalid(string currencyCode)
  {
    var validator = new CreateTenantCommandValidator();
    var command = new CreateTenantCommand("mitienda", "Mi Tienda SRL", "owner@mitienda.com", "Juan Perez", "basic", currencyCode);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTenantCommand.CurrencyCode));
  }

  [Fact]
  public void CreateValidator_ShouldPassWithValidData()
  {
    var validator = new CreateTenantCommandValidator();
    var command = new CreateTenantCommand("mitienda", "Mi Tienda SRL", "owner@mitienda.com", "Juan Perez", "basic", "usd");

    var result = validator.Validate(command);

    result.IsValid.Should().BeTrue();
  }

  // ─── UpdateStoreCommandValidator ──────────────────────────────────────────

  [Fact]
  public void UpdateStoreValidator_ShouldFailWhenNameEmpty()
  {
    var validator = new UpdateStoreCommandValidator();
    var command = new UpdateStoreCommand("", "America/Asuncion", null, null, null, null);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStoreCommand.Name));
  }

  [Fact]
  public void UpdateStoreValidator_ShouldFailWhenPrimaryColorNotHex()
  {
    var validator = new UpdateStoreCommandValidator();
    var command = new UpdateStoreCommand("Mi Tienda", "America/Asuncion", "red", null, null, null);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStoreCommand.PrimaryColor));
  }

  [Fact]
  public void UpdateStoreValidator_ShouldFailWhenLogoUrlNotAbsolute()
  {
    var validator = new UpdateStoreCommandValidator();
    var command = new UpdateStoreCommand("Mi Tienda", "America/Asuncion", null, "not-a-url", null, null);

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStoreCommand.LogoUrl));
  }

  [Fact]
  public void UpdateStoreValidator_ShouldPassWithValidData()
  {
    var validator = new UpdateStoreCommandValidator();
    var command = new UpdateStoreCommand("Mi Tienda", "America/Asuncion", "#FF5733", "https://cdn.example.com/logo.png", "#FFFFFF", "Una tienda de ejemplo");

    var result = validator.Validate(command);

    result.IsValid.Should().BeTrue();
  }
}
