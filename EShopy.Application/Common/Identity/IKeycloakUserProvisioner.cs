using EShopy.Domain.Tenants;

namespace EShopy.Application.Common.Identity;

/// <summary>Provisiona usuarios en Keycloak. Implementacion real llama a la Admin API.</summary>
public interface IKeycloakUserProvisioner
{
  /// <summary>Crea un usuario de tenant con el rol de realm correspondiente.</summary>
  Task<KeycloakUserProvisioningResult> CreateUserAsync(string email, string name, string subdomain, TenantUserRole role, CancellationToken ct);
}

/// <summary>
/// Resultado de crear un usuario en Keycloak. `TemporaryPassword` es la unica vez que el
/// backend la conoce en texto plano (Keycloak la guarda hasheada) — quien llame a esto es
/// responsable de entregarla y no debe persistirla (ver D-06, `agents/backend/BACKLOG.md`).
/// </summary>
public sealed record KeycloakUserProvisioningResult(string UserId, string TemporaryPassword);
