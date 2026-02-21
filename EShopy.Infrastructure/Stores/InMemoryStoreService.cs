using EShopy.Application.Common.Stores;

namespace EShopy.Infrastructure.Stores;

/// <summary>
/// Implementación temporal de IStoreService para el MVP.
/// Retorna un store con ID fijo y moneda PYG hasta que el módulo Stores esté implementado.
/// TODO: Reemplazar por EfStoreService cuando exista la tabla Stores.
/// </summary>
public sealed class InMemoryStoreService : IStoreService
{
  // ID fijo para el store placeholder — permite que StoreId sea consistente entre llamadas
  private static readonly Guid DefaultStoreId = new("11111111-1111-1111-1111-111111111111");

  public Task<StoreDto?> GetDefaultStoreAsync(Guid tenantId, CancellationToken ct)
  {
    return Task.FromResult<StoreDto?>(new StoreDto(DefaultStoreId, "PYG"));
  }
}
