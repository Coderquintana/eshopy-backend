namespace EShopy.Application.Common.Audit;

/// <summary>
/// Registra una operacion sensible (F9-03). El usuario que ejecuta la accion se resuelve
/// internamente en la implementacion (via el contexto de autenticacion) — el caller solo declara
/// que tenant/entidad afecta. Nunca debe hacer fallar la operacion que audita: un problema
/// escribiendo el log no debe revertir una accion de negocio legitima.
/// </summary>
public interface IAuditLogger
{
  Task LogAsync(Guid? tenantId, string action, string entityType, Guid entityId, string? details, CancellationToken ct);
}
