using EShopy.Application.Common.Audit;

namespace EShopy.Tests.Integration.Support;

/// <summary>Guarda las entradas en memoria (visibles para asserts) en vez de tocar SQL Server real.</summary>
internal sealed class InMemoryAuditLogger : IAuditLogger
{
  private readonly object _sync = new();
  public List<(Guid? TenantId, string Action, string EntityType, Guid EntityId, string? Details)> Entries { get; } = [];

  public Task LogAsync(Guid? tenantId, string action, string entityType, Guid entityId, string? details, CancellationToken ct)
  {
    lock (_sync)
    {
      Entries.Add((tenantId, action, entityType, entityId, details));
    }
    return Task.CompletedTask;
  }
}
