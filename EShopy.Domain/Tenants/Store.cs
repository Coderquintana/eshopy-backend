using EShopy.Domain.Common.Entities;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using System.Net.Mail;

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
    string? contactWhatsapp,
    string? contactEmail,
    string? instagramUrl,
    string? facebookUrl,
    string? address,
    string? businessHours,
    DateTime createdAtUtc,
    DateTime? updatedAtUtc,
    string? data)
    : base(id, tenantId, createdAtUtc, createdBy: null, updatedAtUtc, updatedBy: null, data)
  {
    Name = name;
    CurrencyCode = currencyCode;
    Timezone = timezone;
    PrimaryColor = primaryColor;
    LogoUrl = logoUrl;
    BackgroundColor = backgroundColor;
    Description = description;
    ContactWhatsapp = contactWhatsapp;
    ContactEmail = contactEmail;
    InstagramUrl = instagramUrl;
    FacebookUrl = facebookUrl;
    Address = address;
    BusinessHours = businessHours;
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
  public string? ContactWhatsapp { get; private set; }
  public string? ContactEmail { get; private set; }
  public string? InstagramUrl { get; private set; }
  public string? FacebookUrl { get; private set; }
  public string? Address { get; private set; }
  public string? BusinessHours { get; private set; }

  public StoreTheme? Theme => GetData<StoreTheme>();

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
      contactWhatsapp: null,
      contactEmail: null,
      instagramUrl: null,
      facebookUrl: null,
      address: null,
      businessHours: null,
      createdAtUtc,
      createdAtUtc,
      data: null);
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

  public void UpdateContactInformation(
    string? contactWhatsapp,
    string? contactEmail,
    string? instagramUrl,
    string? facebookUrl,
    string? address,
    string? businessHours,
    DateTime updatedAtUtc)
  {
    EnsurePhone(contactWhatsapp);
    EnsureEmail(contactEmail);
    EnsureAbsoluteHttpUrl(instagramUrl, "Instagram");
    EnsureAbsoluteHttpUrl(facebookUrl, "Facebook");
    EnsureMaximumLength(address, 500, "La dirección");
    EnsureMaximumLength(businessHours, 500, "El horario de atención");

    ContactWhatsapp = NormalizeOptional(contactWhatsapp);
    ContactEmail = NormalizeOptional(contactEmail);
    InstagramUrl = NormalizeOptional(instagramUrl);
    FacebookUrl = NormalizeOptional(facebookUrl);
    Address = NormalizeOptional(address);
    BusinessHours = NormalizeOptional(businessHours);
    UpdatedAtUtc = updatedAtUtc;
  }

  public void UpdateTheme(StoreTheme theme, DateTime updatedAtUtc)
  {
    EnsureAllowed(theme.FontFamily, StoreTheme.AllowedFontFamilies, "La tipografía");
    EnsureAllowed(theme.HeadingScale, StoreTheme.AllowedHeadingScales, "La escala de títulos");
    EnsureAllowed(theme.BorderRadius, StoreTheme.AllowedBorderRadii, "El estilo de bordes");
    EnsureAllowed(theme.SpacingDensity, StoreTheme.AllowedSpacingDensities, "La densidad de espaciado");

    var normalized = new StoreTheme(
      NormalizeAllowed(theme.FontFamily, StoreTheme.AllowedFontFamilies),
      NormalizeAllowed(theme.HeadingScale, StoreTheme.AllowedHeadingScales),
      NormalizeAllowed(theme.BorderRadius, StoreTheme.AllowedBorderRadii),
      NormalizeAllowed(theme.SpacingDensity, StoreTheme.AllowedSpacingDensities),
      NormalizeOptional(theme.HeroImageUrl));

    if (normalized is { FontFamily: null, HeadingScale: null, BorderRadius: null, SpacingDensity: null, HeroImageUrl: null })
      Data = null;
    else
      SetData(normalized);

    UpdatedAtUtc = updatedAtUtc;
  }

  public void SetLogoUrl(string? logoUrl, DateTime updatedAtUtc)
  {
    LogoUrl = NormalizeOptional(logoUrl);
    UpdatedAtUtc = updatedAtUtc;
  }

  public void SetHeroImageUrl(string? heroImageUrl, DateTime updatedAtUtc)
  {
    var theme = Theme ?? new StoreTheme(null, null, null, null);
    UpdateTheme(theme with { HeroImageUrl = NormalizeOptional(heroImageUrl) }, updatedAtUtc);
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

  private static void EnsurePhone(string? phone)
  {
    var normalized = NormalizeOptional(phone);
    if (normalized is null)
      return;

    if (normalized.Length > 32 || normalized.Count(char.IsAsciiDigit) < 7 ||
        normalized.Any(character => !char.IsAsciiDigit(character) && character is not '+' and not '-' and not ' ' and not '(' and not ')'))
      throw new DomainException(ErrorCodes.ValidationError, "El WhatsApp de contacto no tiene un formato válido.");
  }

  private static void EnsureEmail(string? email)
  {
    var normalized = NormalizeOptional(email);
    if (normalized is null)
      return;

    if (normalized.Length > 254 || !MailAddress.TryCreate(normalized, out _))
      throw new DomainException(ErrorCodes.ValidationError, "El email de contacto no tiene un formato válido.");
  }

  private static void EnsureAbsoluteHttpUrl(string? url, string fieldName)
  {
    var normalized = NormalizeOptional(url);
    if (normalized is null)
      return;

    if (normalized.Length > 500 || !Uri.TryCreate(normalized, UriKind.Absolute, out var parsed) ||
        parsed.Scheme is not ("http" or "https"))
      throw new DomainException(ErrorCodes.ValidationError, $"La URL de {fieldName} debe ser una URL HTTP o HTTPS válida.");
  }

  private static void EnsureMaximumLength(string? value, int maximumLength, string fieldName)
  {
    if (NormalizeOptional(value)?.Length > maximumLength)
      throw new DomainException(ErrorCodes.ValidationError, $"{fieldName} no puede exceder {maximumLength} caracteres.");
  }

  private static void EnsureAllowed(string? value, IReadOnlySet<string> allowedValues, string fieldName)
  {
    var normalized = NormalizeOptional(value);
    if (normalized is not null && !allowedValues.Contains(normalized))
      throw new DomainException(ErrorCodes.ValidationError, $"{fieldName} no es una opción permitida.");
  }

  private static string? NormalizeAllowed(string? value, IReadOnlySet<string> allowedValues)
  {
    var normalized = NormalizeOptional(value);
    return normalized is null
      ? null
      : allowedValues.Single(candidate => candidate.Equals(normalized, StringComparison.OrdinalIgnoreCase));
  }

  private static string? NormalizeOptional(string? value)
    => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
