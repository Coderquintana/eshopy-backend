using EShopy.Domain.Common.Entities;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Domain.Tenants;

/// <summary>1 Store por Tenant en MVP. CurrencyCode es la moneda heredada por productos y pedidos.</summary>
public sealed class Store : AppEntity
{
  private Store(Guid id,
    Guid tenantId,
    string name,
    string currencyCode,
    string timezone,
    string? primaryColor,
    string? logoUrl,
    string? backgroundColor,
    string? description,
    DateTime createdAtUtc,
    DateTime? updatedAtUtc)
    : base(id, tenantId, createdAtUtc, createdBy: null, updatedAtUtc, updatedBy: null, data: null)
  {
    Name = name;
    CurrencyCode = currencyCode;
    Timezone = timezone;
    PrimaryColor = primaryColor;
    LogoUrl = logoUrl;
    BackgroundColor = backgroundColor;
    Description = description;
  }

  public string Name { get; private set; }

  /// <summary>
  /// Moneda del store. Inmutable tras la creacion: cambiarla rompe precios ya registrados en Products/Orders.
  /// Hoy representa tanto la moneda de exhibicion como la de liquidacion; si esos conceptos se separan,
  /// este es el punto de dominio a dividir.
  /// </summary>
  public string CurrencyCode { get; private set; }
  public string Timezone { get; private set; }
  public string? PrimaryColor { get; private set; }
  public string? LogoUrl { get; private set; }
  public string? BackgroundColor { get; private set; }
  public string? Description { get; private set; }

  public static Store CreateDefault(Guid tenantId, string name, string currencyCode, DateTime createdAtUtc)
  {
    EnsureName(name);
    EnsureCurrencyCode(currencyCode);

    return new Store(Guid.NewGuid(),
      tenantId,
      name.Trim(),
      currencyCode.Trim().ToUpperInvariant(),
      timezone: "America/Asuncion",
      primaryColor: null,
      logoUrl: null,
      backgroundColor: null,
      description: null,
      createdAtUtc,
      createdAtUtc);
  }

  public void UpdateProfile(string name,
    string timezone,
    string? primaryColor,
    string? logoUrl,
    string? backgroundColor,
    string? description,
    DateTime updatedAtUtc)
  {
    EnsureName(name);
    EnsureTimezone(timezone);

    Name = name.Trim();
    Timezone = timezone.Trim();
    PrimaryColor = NormalizeOptional(primaryColor);
    LogoUrl = NormalizeOptional(logoUrl);
    BackgroundColor = NormalizeOptional(backgroundColor);
    Description = NormalizeOptional(description);
    UpdatedAtUtc = updatedAtUtc;
  }

  private static void EnsureName(string name)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new DomainException(ErrorCodes.ValidationError, "El nombre de la tienda es obligatorio.");

    if (name.Trim().Length > 200)
      throw new DomainException(ErrorCodes.ValidationError, "El nombre de la tienda no puede exceder 200 caracteres.");
  }

  private static void EnsureCurrencyCode(string currencyCode)
  {
    if (string.IsNullOrWhiteSpace(currencyCode))
      throw new DomainException(ErrorCodes.ValidationError, "La moneda de la tienda es obligatoria.");

    var normalized = currencyCode.Trim();
    if (normalized.Length != 3 || normalized.Any(character => !char.IsAsciiLetter(character)))
      throw new DomainException(ErrorCodes.ValidationError, "La moneda de la tienda debe tener exactamente 3 letras (ISO 4217).");
  }

  private static void EnsureTimezone(string timezone)
  {
    if (string.IsNullOrWhiteSpace(timezone))
      throw new DomainException(ErrorCodes.ValidationError, "El timezone de la tienda es obligatorio.");
  }

  private static string? NormalizeOptional(string? value)
    => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
