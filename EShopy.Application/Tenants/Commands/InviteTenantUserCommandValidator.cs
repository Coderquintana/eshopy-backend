using FluentValidation;

namespace EShopy.Application.Tenants.Commands;

public sealed class InviteTenantUserCommandValidator : AbstractValidator<InviteTenantUserCommand>
{
  // Owner no es invitable: se crea una unica vez durante el onboarding.
  private static readonly string[] AllowedRoles = ["admin", "staff"];

  public InviteTenantUserCommandValidator()
  {
    RuleFor(x => x.Email)
      .NotEmpty()
      .WithMessage("El email es obligatorio.")
      .EmailAddress()
      .WithMessage("El email no tiene un formato valido.");

    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("El nombre es obligatorio.")
      .MaximumLength(200)
      .WithMessage("El nombre no puede exceder 200 caracteres.");

    RuleFor(x => x.Role)
      .NotEmpty()
      .WithMessage("El rol es obligatorio.")
      .Must(role => AllowedRoles.Contains(role.ToLowerInvariant()))
      .WithMessage("El rol debe ser 'admin' o 'staff'.");
  }
}
