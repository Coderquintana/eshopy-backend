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
}
