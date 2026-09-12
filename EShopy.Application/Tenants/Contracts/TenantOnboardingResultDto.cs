namespace EShopy.Application.Tenants.Contracts;

public sealed class TenantOnboardingResultDto
{
  public required Guid TenantId { get; init; }
  public required string Subdomain { get; init; }
  public required string Status { get; init; }

  /// <summary>
  /// Password temporal del Owner en Keycloak (`temporary: true`, fuerza a cambiarla en el
  /// primer login). El backend la devuelve UNA SOLA VEZ, aca: no se persiste en texto plano
  /// ni se puede recuperar despues — quien la reciba es responsable de entregarsela al Owner
  /// (hoy a mano, no hay SMTP configurado; ver D-06 en `agents/backend/BACKLOG.md`).
  /// </summary>
  public required string OwnerTemporaryPassword { get; init; }
}
