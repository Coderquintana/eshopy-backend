namespace EShopy.Application.Common.Identity;

/// <summary>Provisiona usuarios en Keycloak. Implementacion real llama a la Admin API.</summary>
public interface IKeycloakUserProvisioner
{
  /// <summary>Crea el usuario Owner de un tenant nuevo con rol TENANT_OWNER. Retorna su KeycloakUserId.</summary>
  Task<string> CreateOwnerUserAsync(string email, string name, string subdomain, CancellationToken ct);
}
