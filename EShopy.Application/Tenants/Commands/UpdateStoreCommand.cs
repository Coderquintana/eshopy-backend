namespace EShopy.Application.Tenants.Commands;

public sealed record UpdateStoreCommand(
  string Name,
  string Timezone,
  string? PrimaryColor,
  string? LogoUrl,
  string? BackgroundColor,
  string? Description);
