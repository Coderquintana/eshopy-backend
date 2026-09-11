namespace EShopy.Application.Tenants.Contracts;

/// <summary>Configuracion publica de la tienda. Misma forma para GET publico y respuesta de PUT admin.</summary>
public sealed class StoreProfileDto
{
  public required Guid StoreId { get; init; }
  public required string Name { get; init; }
  public required string CurrencyCode { get; init; }
  public required string Timezone { get; init; }
  public string? PrimaryColor { get; init; }
  public string? LogoUrl { get; init; }
  public string? BackgroundColor { get; init; }
  public string? Description { get; init; }
  public string? ContactWhatsapp { get; init; }
  public string? ContactEmail { get; init; }
  public string? InstagramUrl { get; init; }
  public string? FacebookUrl { get; init; }
  public string? Address { get; init; }
  public string? BusinessHours { get; init; }
  public string? FontFamily { get; init; }
  public string? HeadingScale { get; init; }
  public string? BorderRadius { get; init; }
  public string? SpacingDensity { get; init; }
}
