using EShopy.Domain.Tenants;

namespace EShopy.Application.Common.Identity;

/// <summary>Provisiona usuarios en Keycloak. Implementacion real llama a la Admin API.</summary>
public interface IKeycloakUserProvisioner
{
  /// <summary>Crea un usuario de tenant con el rol de realm correspondiente. Retorna su KeycloakUserId.</summary>
  Task<string> CreateUserAsync(string email, string name, string subdomain, TenantUserRole role, CancellationToken ct);
}
