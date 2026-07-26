using FluentValidation;

namespace EShopy.Application.Tenants.Commands;

public sealed class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
{
  private const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";

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
  }
}
