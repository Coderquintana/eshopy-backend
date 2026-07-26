using EShopy.Application.Common.Stores;

namespace EShopy.Tests.Integration.Support;

internal sealed class FakeStoreService : IStoreService
{
  public static readonly Guid StoreId = Guid.Parse("22222222-2222-2222-2222-222222222222");

  public Task<StoreDto?> GetDefaultStoreAsync(Guid tenantId, CancellationToken ct)
    => Task.FromResult<StoreDto?>(new StoreDto(StoreId, "PYG"));
}
