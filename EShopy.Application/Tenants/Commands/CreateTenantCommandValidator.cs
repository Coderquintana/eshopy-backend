using FluentValidation;

namespace EShopy.Application.Tenants.Commands;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
  private static readonly string[] AllowedPlans = ["basic", "gold", "diamond"];

  public CreateTenantCommandValidator()
  {
    RuleFor(x => x.Subdomain)
      .NotEmpty()
      .WithMessage("El subdominio es obligatorio.")
      .Length(3, 50)
      .WithMessage("El subdominio debe tener entre 3 y 50 caracteres.")
      .Matches(@"^[a-z0-9-]+$")
      .WithMessage("El subdominio solo puede contener letras minúsculas, números y guiones.");

    RuleFor(x => x.BusinessName)
      .NotEmpty()
      .WithMessage("El nombre del negocio es obligatorio.")
      .MaximumLength(200)
      .WithMessage("El nombre del negocio no puede exceder 200 caracteres.");

    RuleFor(x => x.OwnerEmail)
      .NotEmpty()
      .WithMessage("El email del owner es obligatorio.")
      .EmailAddress()
      .WithMessage("El email del owner no tiene un formato valido.");

    RuleFor(x => x.OwnerName)
      .NotEmpty()
      .WithMessage("El nombre del owner es obligatorio.")
      .MaximumLength(200)
      .WithMessage("El nombre del owner no puede exceder 200 caracteres.");

    RuleFor(x => x.Plan)
      .NotEmpty()
      .WithMessage("El plan es obligatorio.")
      .Must(plan => AllowedPlans.Contains(plan.ToLowerInvariant()))
      .WithMessage("El plan debe ser 'basic', 'gold' o 'diamond'.");
  }
}
