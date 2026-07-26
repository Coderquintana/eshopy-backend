using EShopy.Application.Common.Audit;
using EShopy.Application.Common.Context;
using EShopy.Application.Common.Identity;
using EShopy.Application.Tenants.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Common.Results;
using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants.Commands;

public sealed class InviteTenantUserCommandHandler(
  ITenantUserRepository repository,
  IKeycloakUserProvisioner keycloakProvisioner,
  TenantContext tenantContext,
  IAuditLogger auditLogger)
{
  private readonly InviteTenantUserCommandValidator _validator = new();

  public async Task<Result<TenantUserDto>> Handle(InviteTenantUserCommand command, CancellationToken ct)
  {
    // 1. Validación de entrada
    var validation = _validator.Validate(command);
    if (!validation.IsValid)
    {
      var msg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      return Result<TenantUserDto>.Fail(ErrorCodes.ValidationError, msg);
    }

    if (!tenantContext.TenantId.HasValue || tenantContext.Subdomain is null)
      return Result<TenantUserDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var tenantId = tenantContext.TenantId.Value;
    var normalizedEmail = command.Email.Trim().ToLowerInvariant();
    var role = ParseRole(command.Role);

    // 2. Unicidad de email dentro del tenant
    if (await repository.EmailExistsForTenantAsync(tenantId, normalizedEmail, ct))
      return Result<TenantUserDto>.Fail(ErrorCodes.Conflict, "Ya existe un usuario con ese email en este tenant.");

    // 3. Crear el usuario en Keycloak ANTES de escribir en la base local
    var keycloakUserId = await keycloakProvisioner.CreateUserAsync(
      normalizedEmail, command.Name, tenantContext.Subdomain, role, ct);

    // 4. Crear y persistir el TenantUser
    try
    {
      var tenantUser = TenantUser.Create(tenantId, keycloakUserId, normalizedEmail, command.Name, role, DateTime.UtcNow);
      await repository.AddAsync(tenantUser, ct);
      await auditLogger.LogAsync(tenantId, "TenantUser.Invite", "TenantUser", tenantUser.Id, $"{normalizedEmail} ({role})", ct);
      return Result<TenantUserDto>.Ok(TenantMappings.ToUserDto(tenantUser));
    }
    catch (DomainException ex)
    {
      return Result<TenantUserDto>.Fail(ex.Code, ex.Message);
    }
  }

  private static TenantUserRole ParseRole(string role) => role.Trim().ToLowerInvariant() switch
  {
    "admin" => TenantUserRole.Admin,
    "staff" => TenantUserRole.Staff,
    _ => throw new DomainException(ErrorCodes.ValidationError, $"Rol desconocido: {role}.")
  };
}
