namespace EShopy.Domain.Common.Counters;

/// <summary>
/// Contador atomico por tenant (PK compuesta TenantId+CounterType). CurrentValue es concurrency
/// token en EF — asi se genera secuencias (ej. OrderNumber) sin SQL crudo, con reintento en el
/// caller si dos operaciones compiten por el mismo contador. Ver ICheckoutWriter.
/// </summary>
public sealed class TenantCounter
{
  public const string OrderNumberType = "OrderNumber";

  private TenantCounter(Guid tenantId, string counterType, int currentValue)
  {
    TenantId = tenantId;
    CounterType = counterType;
    CurrentValue = currentValue;
  }

  public Guid TenantId { get; private set; }
  public string CounterType { get; private set; }
  public int CurrentValue { get; private set; }

  public static TenantCounter Create(Guid tenantId, string counterType) => new(tenantId, counterType, 0);

  public void Increment() => CurrentValue++;
}
