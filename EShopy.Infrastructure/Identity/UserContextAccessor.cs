using System.Security.Claims;
using System.Text.Json;
using EShopy.Application.Common.Identity;
using Microsoft.AspNetCore.Http;

namespace EShopy.Infrastructure.Identity;

public sealed class UserContextAccessor(IHttpContextAccessor httpContextAccessor)
{
  public UserContext GetUserContext()
  {
    var user = httpContextAccessor.HttpContext?.User;
    if (user?.Identity?.IsAuthenticated != true)
      return new UserContext();

    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
      ?? user.FindFirstValue("sub")
      ?? string.Empty;

    var email = user.FindFirstValue(ClaimTypes.Email)
      ?? user.FindFirstValue("email")
      ?? string.Empty;

    var displayName = user.FindFirstValue(ClaimTypes.Name)
      ?? user.FindFirstValue("name")
      ?? user.FindFirstValue("preferred_username")
      ?? email;

    var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var claim in user.Claims)
    {
      if (claim.Type is ClaimTypes.Role or "roles")
        roles.Add(claim.Value);

      if (claim.Type is "permissions")
        permissions.Add(claim.Value);
    }

    ExtractRealmAccessRoles(user.FindFirstValue("realm_access"), roles);
    ExtractResourceAccessRoles(user.FindFirstValue("resource_access"), roles);

    Guid? tenantId = null;
    var tenantIdClaim = user.FindFirstValue("tenant_id");
    if (Guid.TryParse(tenantIdClaim, out var parsedTenantId))
      tenantId = parsedTenantId;

    return new UserContext
    {
      UserId = userId,
      Email = email,
      DisplayName = displayName,
      Roles = roles.ToList(),
      Permissions = permissions.ToList(),
      TenantId = tenantId
    };
  }

  private static void ExtractRealmAccessRoles(string? realmAccessValue, ISet<string> roles)
  {
    if (string.IsNullOrWhiteSpace(realmAccessValue))
      return;

    try
    {
      using var json = JsonDocument.Parse(realmAccessValue);
      if (!json.RootElement.TryGetProperty("roles", out var rolesNode) || rolesNode.ValueKind != JsonValueKind.Array)
        return;

      foreach (var roleNode in rolesNode.EnumerateArray())
      {
        var role = roleNode.GetString();
        if (!string.IsNullOrWhiteSpace(role))
          roles.Add(role);
      }
    }
    catch (JsonException)
    {
      // Ignorado: token con claim realm_access no JSON.
    }
  }

  private static void ExtractResourceAccessRoles(string? resourceAccessValue, ISet<string> roles)
  {
    if (string.IsNullOrWhiteSpace(resourceAccessValue))
      return;

    try
    {
      using var json = JsonDocument.Parse(resourceAccessValue);
      foreach (var clientNode in json.RootElement.EnumerateObject())
      {
        if (!clientNode.Value.TryGetProperty("roles", out var rolesNode) || rolesNode.ValueKind != JsonValueKind.Array)
          continue;

        foreach (var roleNode in rolesNode.EnumerateArray())
        {
          var role = roleNode.GetString();
          if (!string.IsNullOrWhiteSpace(role))
            roles.Add(role);
        }
      }
    }
    catch (JsonException)
    {
      // Ignorado: token con claim resource_access no JSON.
    }
  }
}
