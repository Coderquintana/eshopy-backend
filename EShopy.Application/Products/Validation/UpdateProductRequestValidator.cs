using FluentValidation;
using EShopy.Application.Products.Requests;

namespace EShopy.Application.Products.Validation;

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
  public UpdateProductRequestValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("El nombre del producto es obligatorio.")
      .MaximumLength(200)
      .WithMessage("El nombre del producto no puede exceder 200 caracteres.");

    RuleFor(x => x.Price)
      .GreaterThanOrEqualTo(0)
      .WithMessage("El precio del producto debe ser mayor o igual a cero.");

    RuleFor(x => x.StockOnHand)
      .GreaterThanOrEqualTo(0)
      .WithMessage("El stock del producto debe ser mayor o igual a cero.");
  }
}
