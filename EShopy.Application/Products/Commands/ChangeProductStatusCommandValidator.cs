using FluentValidation;

namespace EShopy.Application.Products.Commands;

public sealed class ChangeProductStatusCommandValidator : AbstractValidator<ChangeProductStatusCommand>
{
  public ChangeProductStatusCommandValidator()
  {
    RuleFor(x => x.Status)
      .IsInEnum()
      .WithMessage("El estado del producto no es válido.");
  }
}
