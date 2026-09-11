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
    var command = new UpdateStoreCommand(
      "Mi Tienda",
      "America/Asuncion",
      "#FF5733",
      "https://cdn.example.com/logo.png",
      "#FFFFFF",
      "Una tienda de ejemplo",
      "+595 981 123456",
      "ventas@mitienda.com",
      "https://instagram.com/mitienda",
      "https://facebook.com/mitienda",
      "Asunción, Paraguay",
      "Lun a sáb, 09:00 a 18:00",
      "Georgia",
      "large",
      "rounded",
      "spacious");

    var result = validator.Validate(command);

    result.IsValid.Should().BeTrue();
  }

  [Theory]
  [InlineData("fontFamily", "Comic Sans MS")]
  [InlineData("headingScale", "huge")]
  [InlineData("borderRadius", "pill")]
  [InlineData("spacingDensity", "dense")]
  public void UpdateStoreValidator_ShouldFailWhenThemeOptionIsUnknown(string field, string value)
  {
    var validator = new UpdateStoreCommandValidator();
    var command = ValidUpdateStoreCommand() with
    {
      FontFamily = field == "fontFamily" ? value : "Inter",
      HeadingScale = field == "headingScale" ? value : "normal",
      BorderRadius = field == "borderRadius" ? value : "rounded",
      SpacingDensity = field == "spacingDensity" ? value : "normal"
    };

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(error => error.PropertyName.Equals(field, StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void UpdateStoreValidator_ShouldFailWhenContactDataIsInvalid()
  {
    var validator = new UpdateStoreCommandValidator();
    var command = ValidUpdateStoreCommand() with
    {
      ContactWhatsapp = "123",
      ContactEmail = "not-an-email",
      InstagramUrl = "ftp://instagram.com/mitienda"
    };

    var result = validator.Validate(command);

    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateStoreCommand.ContactWhatsapp));
    result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateStoreCommand.ContactEmail));
    result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateStoreCommand.InstagramUrl));
  }

  private static UpdateStoreCommand ValidUpdateStoreCommand() => new(
    "Mi Tienda",
    "America/Asuncion",
    "#FF5733",
    null,
    "#FFFFFF",
    null);
}
