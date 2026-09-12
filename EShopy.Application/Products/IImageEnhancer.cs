namespace EShopy.Application.Products;

/// <summary>Puerto separado del storage para una mejora de imagen premium futura.</summary>
public interface IImageEnhancer
{
  Task<Stream> EnhanceAsync(Stream content, string contentType, CancellationToken ct);
}
