using FluentValidation;
using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants.Commands;

public sealed class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
{
  private const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";
  private const string WhatsappPattern = "^[+0-9() -]+$";

  public UpdateStoreCommandValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("El nombre de la tienda es obligatorio.")
      .MaximumLength(200)
      .WithMessage("El nombre de la tienda no puede exceder 200 caracteres.");

    RuleFor(x => x.Timezone)
      .NotEmpty()
      .WithMessage("El timezone es obligatorio.");

    RuleFor(x => x.PrimaryColor)
      .Matches(HexColorPattern)
      .WithMessage("El color primario debe ser un hex valido, ej. '#FF5733'.")
      .When(x => !string.IsNullOrWhiteSpace(x.PrimaryColor));

    RuleFor(x => x.BackgroundColor)
      .Matches(HexColorPattern)
      .WithMessage("El color de fondo debe ser un hex valido, ej. '#FFFFFF'.")
      .When(x => !string.IsNullOrWhiteSpace(x.BackgroundColor));

    RuleFor(x => x.LogoUrl)
      .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
      .WithMessage("El logo debe ser una URL valida.")
      .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));

    RuleFor(x => x.Description)
      .MaximumLength(1000)
      .WithMessage("La descripción no puede exceder 1000 caracteres.")
      .When(x => !string.IsNullOrWhiteSpace(x.Description));

    RuleFor(x => x.ContactWhatsapp)
      .MaximumLength(32)
      .WithMessage("El WhatsApp de contacto no puede exceder 32 caracteres.")
      .Matches(WhatsappPattern)
      .WithMessage("El WhatsApp de contacto no tiene un formato válido.")
      .Must(value => value!.Count(char.IsAsciiDigit) >= 7)
      .WithMessage("El WhatsApp de contacto debe tener al menos 7 dígitos.")
      .When(x => !string.IsNullOrWhiteSpace(x.ContactWhatsapp));

    RuleFor(x => x.ContactEmail)
      .MaximumLength(254)
      .WithMessage("El email de contacto no puede exceder 254 caracteres.")
      .EmailAddress()
      .WithMessage("El email de contacto no tiene un formato válido.")
      .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

    AddOptionalHttpUrlRule(x => x.InstagramUrl, "La URL de Instagram");
    AddOptionalHttpUrlRule(x => x.FacebookUrl, "La URL de Facebook");

    RuleFor(x => x.Address)
      .MaximumLength(500)
      .WithMessage("La dirección no puede exceder 500 caracteres.")
      .When(x => !string.IsNullOrWhiteSpace(x.Address));

    RuleFor(x => x.BusinessHours)
      .MaximumLength(500)
      .WithMessage("El horario de atención no puede exceder 500 caracteres.")
      .When(x => !string.IsNullOrWhiteSpace(x.BusinessHours));

    AddOptionalAllowedValueRule(x => x.FontFamily, StoreTheme.AllowedFontFamilies, "La tipografía");
    AddOptionalAllowedValueRule(x => x.HeadingScale, StoreTheme.AllowedHeadingScales, "La escala de títulos");
    AddOptionalAllowedValueRule(x => x.BorderRadius, StoreTheme.AllowedBorderRadii, "El estilo de bordes");
    AddOptionalAllowedValueRule(x => x.SpacingDensity, StoreTheme.AllowedSpacingDensities, "La densidad de espaciado");
  }

  private void AddOptionalHttpUrlRule(
    System.Linq.Expressions.Expression<Func<UpdateStoreCommand, string?>> selector,
    string fieldName)
  {
    RuleFor(selector)
      .MaximumLength(500)
      .WithMessage($"{fieldName} no puede exceder 500 caracteres.")
      .Must(value => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
      .WithMessage($"{fieldName} debe ser una URL HTTP o HTTPS válida.")
      .When(command => !string.IsNullOrWhiteSpace(selector.Compile()(command)));
  }

  private void AddOptionalAllowedValueRule(
    System.Linq.Expressions.Expression<Func<UpdateStoreCommand, string?>> selector,
    IReadOnlySet<string> allowedValues,
    string fieldName)
  {
    RuleFor(selector)
      .Must(value => value is null || allowedValues.Contains(value.Trim()))
      .WithMessage($"{fieldName} no es una opción permitida.")
      .When(command => !string.IsNullOrWhiteSpace(selector.Compile()(command)));
  }
}
