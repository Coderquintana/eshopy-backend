using FluentValidation;
using EShopy.Application.Products.Requests;

namespace EShopy.Application.Products.Validation;

public sealed class ChangeProductStatusRequestValidator : AbstractValidator<ChangeProductStatusRequest>
{
  public ChangeProductStatusRequestValidator()
  {
    RuleFor(x => x.Status)
      .IsInEnum()
      .WithMessage("El estado del producto no es válido.");
  }
}
