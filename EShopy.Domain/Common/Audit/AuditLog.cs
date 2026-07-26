namespace EShopy.Domain.Common.Audit;

/// <summary>
/// Registro append-only de una operacion sensible (F9-03). Sin invariantes de negocio ni
/// transiciones — es un log, no un agregado. <see cref="TenantId"/> es nullable: algunas acciones
/// son a nivel plataforma (ej. activacion de tenant via SUPERADMIN, antes de que el tenant este
/// operativo).
/// </summary>
public sealed class AuditLog
{
  private AuditLog(
    Guid id, Guid? tenantId, string? userId, string? userEmail,
    string action, string entityType, Guid entityId, string? details, DateTime createdAtUtc)
  {
    Id = id;
    TenantId = tenantId;
    UserId = userId;
    UserEmail = userEmail;
    Action = action;
    EntityType = entityType;
    EntityId = entityId;
    Details = details;
    CreatedAtUtc = createdAtUtc;
  }

  public Guid Id { get; private set; }
  public Guid? TenantId { get; private set; }
  public string? UserId { get; private set; }
  public string? UserEmail { get; private set; }

  /// <summary>Ej. "Order.ChangeStatus", "Tenant.Activate" — Modulo.Operacion.</summary>
  public string Action { get; private set; }
  public string EntityType { get; private set; }
  public Guid EntityId { get; private set; }

  /// <summary>Texto libre (ej. "PendingPayment -> Paid"), no JSON estructurado — mantenerlo simple hasta que un caso de uso real pida mas.</summary>
  public string? Details { get; private set; }
  public DateTime CreatedAtUtc { get; private set; }

  public static AuditLog Create(
    Guid? tenantId, string? userId, string? userEmail,
    string action, string entityType, Guid entityId, string? details, DateTime createdAtUtc)
    => new(Guid.NewGuid(), tenantId, userId, userEmail, action, entityType, entityId, details, createdAtUtc);
}
