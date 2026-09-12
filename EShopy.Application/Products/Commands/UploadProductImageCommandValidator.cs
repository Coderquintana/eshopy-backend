using FluentValidation;

namespace EShopy.Application.Products.Commands;

public sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
  public UploadProductImageCommandValidator()
  {
    RuleFor(x => x.Id)
      .NotEmpty()
      .WithMessage("El identificador del producto es obligatorio.");

    RuleFor(x => x.FileName)
      .NotEmpty()
      .WithMessage("Seleccioná una imagen.")
      .MaximumLength(255)
      .WithMessage("El nombre del archivo no puede exceder 255 caracteres.");

    RuleFor(x => x.Length)
      .GreaterThan(0)
      .WithMessage("La imagen está vacía.")
      .LessThanOrEqualTo(ProductImageConstraints.MaxFileBytes)
      .WithMessage("La imagen no puede superar 5 MB.");

    RuleFor(x => x.ContentType)
      .Must(ProductImageConstraints.IsAllowedContentType)
      .WithMessage("Usá una imagen JPEG, PNG o WebP.");

    RuleFor(x => x.RowVersion)
      .NotEmpty()
      .WithMessage("La versión del producto es obligatoria.");

    RuleFor(x => x.RowVersion)
      .Must(ProductConcurrency.IsValidToken)
      .WithMessage("La versión del producto no es válida.")
      .When(x => !string.IsNullOrWhiteSpace(x.RowVersion));
  }
}
