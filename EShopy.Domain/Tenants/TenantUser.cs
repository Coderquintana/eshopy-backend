using EShopy.Domain.Common.Entities;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Domain.Tenants;

/// <summary>Usuario con acceso al panel de administracion de un tenant. Email unico por tenant.</summary>
public sealed class TenantUser : AppEntity
{
  private TenantUser(Guid id,
    Guid tenantId,
    string keycloakUserId,
    string email,
    string name,
    TenantUserRole role,
    bool isActive,
    DateTime createdAtUtc,
    DateTime? updatedAtUtc)
    : base(id, tenantId, createdAtUtc, createdBy: null, updatedAtUtc, updatedBy: null, data: null)
  {
    KeycloakUserId = keycloakUserId;
    Email = email;
    Name = name;
    Role = role;
    IsActive = isActive;
  }

  public string KeycloakUserId { get; private set; }
  public string Email { get; private set; }
  public string Name { get; private set; }
  public TenantUserRole Role { get; private set; }
  public bool IsActive { get; private set; }

  public static TenantUser Create(Guid tenantId, string keycloakUserId, string email, string name, TenantUserRole role, DateTime createdAtUtc)
  {
    EnsureKeycloakUserId(keycloakUserId);
    var normalizedEmail = NormalizeEmail(email);
    EnsureName(name);

    return new TenantUser(Guid.NewGuid(),
      tenantId,
      keycloakUserId,
      normalizedEmail,
      name.Trim(),
      role,
      isActive: true,
      createdAtUtc,
      createdAtUtc);
  }

  public void Deactivate(DateTime updatedAtUtc)
  {
    IsActive = false;
    UpdatedAtUtc = updatedAtUtc;
  }

  public void Activate(DateTime updatedAtUtc)
  {
    IsActive = true;
    UpdatedAtUtc = updatedAtUtc;
  }

  private static void EnsureKeycloakUserId(string keycloakUserId)
  {
    if (string.IsNullOrWhiteSpace(keycloakUserId))
      throw new DomainException(ErrorCodes.ValidationError, "El KeycloakUserId es obligatorio.");
  }

  private static string NormalizeEmail(string email)
  {
    if (string.IsNullOrWhiteSpace(email))
      throw new DomainException(ErrorCodes.ValidationError, "El email es obligatorio.");

    return email.Trim().ToLowerInvariant();
  }

  private static void EnsureName(string name)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new DomainException(ErrorCodes.ValidationError, "El nombre es obligatorio.");
  }
}
