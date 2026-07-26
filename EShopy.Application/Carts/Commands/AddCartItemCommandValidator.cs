using FluentValidation;

namespace EShopy.Application.Carts.Commands;

public sealed class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
  public AddCartItemCommandValidator()
  {
    RuleFor(x => x.ProductId)
      .NotEmpty()
      .WithMessage("El producto es obligatorio.");

    RuleFor(x => x.Quantity)
      .GreaterThanOrEqualTo(1)
      .WithMessage("La cantidad debe ser mayor o igual a 1.");
  }
}
