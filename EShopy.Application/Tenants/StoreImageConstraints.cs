namespace EShopy.Application.Tenants;

public static class StoreImageConstraints
{
  public const long MaxFileBytes = 5 * 1024 * 1024;
  public const int LogoMaxDimensionPixels = 800;
  public const int HeroMaxDimensionPixels = 1920;

  private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "image/jpeg",
    "image/png",
    "image/webp"
  };

  public static bool IsAllowedContentType(string? contentType)
    => contentType is not null && AllowedContentTypes.Contains(contentType);
}
