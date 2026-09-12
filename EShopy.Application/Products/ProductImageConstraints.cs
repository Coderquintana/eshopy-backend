namespace EShopy.Application.Products;

public static class ProductImageConstraints
{
  public const long MaxFileBytes = 5 * 1024 * 1024;
  public const int MaxDimensionPixels = 1200;

  private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "image/jpeg",
    "image/png",
    "image/webp"
  };

  public static bool IsAllowedContentType(string? contentType)
    => contentType is not null && AllowedContentTypes.Contains(contentType);
}
