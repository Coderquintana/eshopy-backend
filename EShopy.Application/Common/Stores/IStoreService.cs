namespace EShopy.Application.Common.Stores;

/// <summary>Contrato para obtener información del Store de un tenant.</summary>
public interface IStoreService
{
  /// <summary>Retorna el store principal del tenant o null si no existe.</summary>
  Task<StoreDto?> GetDefaultStoreAsync(Guid tenantId, CancellationToken ct);
}
