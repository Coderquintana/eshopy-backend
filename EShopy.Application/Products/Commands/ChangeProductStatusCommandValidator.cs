using FluentValidation;

namespace EShopy.Application.Products.Commands;

public sealed class ChangeProductStatusCommandValidator : AbstractValidator<ChangeProductStatusCommand>
{
  public ChangeProductStatusCommandValidator()
  {
    RuleFor(x => x.Status)
      .IsInEnum()
      .WithMessage("El estado del producto no es válido.");

    RuleFor(x => x.RowVersion)
      .NotEmpty()
      .WithMessage("La versión del producto es obligatoria.");

    RuleFor(x => x.RowVersion)
      .Must(ProductConcurrency.IsValidToken)
      .WithMessage("La versión del producto no es válida.")
      .When(x => !string.IsNullOrWhiteSpace(x.RowVersion));
  }
}
