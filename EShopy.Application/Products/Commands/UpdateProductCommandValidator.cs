using FluentValidation;

namespace EShopy.Application.Products.Commands;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
  public UpdateProductCommandValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("El nombre del producto es obligatorio.")
      .MaximumLength(200)
      .WithMessage("El nombre del producto no puede exceder 200 caracteres.");

    RuleFor(x => x.Sku)
      .MaximumLength(64)
      .WithMessage("El SKU del producto no puede exceder 64 caracteres.")
      .When(x => !string.IsNullOrWhiteSpace(x.Sku));

    RuleFor(x => x.Description)
      .MaximumLength(5000)
      .WithMessage("La descripción no puede exceder 5000 caracteres.")
      .When(x => !string.IsNullOrWhiteSpace(x.Description));

    RuleFor(x => x.Price)
      .GreaterThanOrEqualTo(0)
      .WithMessage("El precio del producto debe ser mayor o igual a cero.");

    RuleFor(x => x.StockOnHand)
      .GreaterThanOrEqualTo(0)
      .WithMessage("El stock del producto debe ser mayor o igual a cero.");
  }
}
