using FluentValidation;

namespace EShopy.Application.Orders.Commands;

public sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
  public CheckoutCommandValidator()
  {
    RuleFor(x => x.BuyerEmail)
      .NotEmpty()
      .WithMessage("El email del comprador es obligatorio.")
      .EmailAddress()
      .WithMessage("El email del comprador no tiene un formato valido.");

    RuleFor(x => x.BuyerName)
      .NotEmpty()
      .WithMessage("El nombre del comprador es obligatorio.")
      .MaximumLength(200)
      .WithMessage("El nombre del comprador no puede exceder 200 caracteres.");

    RuleFor(x => x.ShippingAddress)
      .MaximumLength(1000)
      .WithMessage("La direccion de entrega no puede exceder 1000 caracteres.")
      .When(x => !string.IsNullOrWhiteSpace(x.ShippingAddress));
  }
}
