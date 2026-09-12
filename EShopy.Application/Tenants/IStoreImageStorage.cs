namespace EShopy.Application.Tenants;

public interface IStoreImageStorage
{
  Task<string> SaveAsync(
    Guid storeId,
    StoreImageKind kind,
    Stream content,
    string fileName,
    string contentType,
    CancellationToken ct);

  Task DeleteAsync(Guid storeId, string imageUrl, CancellationToken ct);
}
