using EShopy.Application.Tenants;
using FluentValidation;

namespace EShopy.Application.Tenants.Commands;

public sealed class UploadStoreImageCommandValidator : AbstractValidator<UploadStoreImageCommand>
{
  public UploadStoreImageCommandValidator()
  {
    RuleFor(x => x.Kind)
      .IsInEnum()
      .WithMessage("El tipo de imagen de la tienda no es válido.");

    RuleFor(x => x.FileName)
      .NotEmpty()
      .WithMessage("Seleccioná una imagen.")
      .MaximumLength(255)
      .WithMessage("El nombre del archivo no puede exceder 255 caracteres.");

    RuleFor(x => x.Length)
      .GreaterThan(0)
      .WithMessage("La imagen está vacía.")
      .LessThanOrEqualTo(StoreImageConstraints.MaxFileBytes)
      .WithMessage("La imagen no puede superar 5 MB.");

    RuleFor(x => x.ContentType)
      .Must(StoreImageConstraints.IsAllowedContentType)
      .WithMessage("Usá una imagen JPEG, PNG o WebP.");
  }
}
