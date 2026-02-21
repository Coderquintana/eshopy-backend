namespace EShopy.Application.Common.Identity;

public sealed class UserContext
{
  public string UserId { get; init; } = string.Empty;
  public string Email { get; init; } = string.Empty;
  public string DisplayName { get; init; } = string.Empty;
  public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
  public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
  public Guid? TenantId { get; init; }

  public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
  public bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
  public bool IsSuperAdmin => IsInRole("ESHOPY_SUPERADMIN");
  public bool IsTenantOwner => IsInRole("TENANT_OWNER");
  public bool IsTenantAdmin => IsInRole("TENANT_ADMIN");
  public bool IsTenantStaff => IsInRole("TENANT_STAFF");
}
