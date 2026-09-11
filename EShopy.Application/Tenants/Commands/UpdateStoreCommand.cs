namespace EShopy.Application.Tenants.Commands;

public sealed record UpdateStoreCommand(
  string Name,
  string Timezone,
  string? PrimaryColor,
  string? LogoUrl,
  string? BackgroundColor,
  string? Description,
  string? ContactWhatsapp = null,
  string? ContactEmail = null,
  string? InstagramUrl = null,
  string? FacebookUrl = null,
  string? Address = null,
  string? BusinessHours = null,
  string? FontFamily = null,
  string? HeadingScale = null,
  string? BorderRadius = null,
  string? SpacingDensity = null);
