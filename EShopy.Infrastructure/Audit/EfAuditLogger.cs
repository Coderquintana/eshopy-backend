using EShopy.Application.Common.Audit;
using EShopy.Domain.Common.Audit;
using EShopy.Infrastructure.Identity;
using EShopy.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace EShopy.Infrastructure.Audit;

/// <summary>
/// Escritura best-effort: nunca tira. Un fallo al auditar no debe revertir ni fallar la operacion
/// de negocio que la disparo (ver IAuditLogger). SaveChangesAsync propio, separado del de la
/// operacion auditada — no es parte de ningun writer atomico, perder ocasionalmente un registro de
/// auditoria por una carrera es aceptable, perder Order/Payment no lo es.
/// </summary>
public sealed class EfAuditLogger(EShopyDbContext db, UserContextAccessor userContextAccessor, ILogger<EfAuditLogger> log) : IAuditLogger
{
  public async Task LogAsync(Guid? tenantId, string action, string entityType, Guid entityId, string? details, CancellationToken ct)
  {
    try
    {
      var user = userContextAccessor.GetUserContext();

      var entry = AuditLog.Create(
        tenantId,
        string.IsNullOrEmpty(user.UserId) ? null : user.UserId,
        string.IsNullOrEmpty(user.Email) ? null : user.Email,
        action, entityType, entityId, details, DateTime.UtcNow);

      db.AuditLogs.Add(entry);
      await db.SaveChangesAsync(ct);
    }
    catch (Exception ex)
    {
      log.LogError(ex, "No se pudo escribir el AuditLog para {Action} sobre {EntityType} {EntityId}", action, entityType, entityId);
    }
  }
}
