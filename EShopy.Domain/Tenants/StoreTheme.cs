namespace EShopy.Domain.Tenants;

/// <summary>Preferencias cosmeticas del storefront, persistidas en Store.Data.</summary>
public sealed record StoreTheme(
  string? FontFamily,
  string? HeadingScale,
  string? BorderRadius,
  string? SpacingDensity,
  string? HeroImageUrl = null)
{
  public static readonly IReadOnlySet<string> AllowedFontFamilies = new HashSet<string>(
    ["Inter", "Georgia", "Trebuchet MS"],
    StringComparer.OrdinalIgnoreCase);

  public static readonly IReadOnlySet<string> AllowedHeadingScales = new HashSet<string>(
    ["compact", "normal", "large"],
    StringComparer.OrdinalIgnoreCase);

  public static readonly IReadOnlySet<string> AllowedBorderRadii = new HashSet<string>(
    ["square", "rounded"],
    StringComparer.OrdinalIgnoreCase);

  public static readonly IReadOnlySet<string> AllowedSpacingDensities = new HashSet<string>(
    ["compact", "normal", "spacious"],
    StringComparer.OrdinalIgnoreCase);
}
