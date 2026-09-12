namespace EShopy.Application.Products;

public interface IProductImageStorage
{
  Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct);
  Task DeleteAsync(string imageUrl, CancellationToken ct);
}
