using EShopy.Domain.Common.Entities;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Domain.Products;

public sealed class Product : AppEntity
{
  private Product(Guid id,
    Guid tenantId,
    string slug,
    string? sku,
    string name,
    string? description,
    decimal price,
    string currencyCode,
    ProductStatus status,
    int stockOnHand,
    DateTime createdAtUtc,
    DateTime? updatedAtUtc)
    : base(id, tenantId, createdAtUtc, createdBy: null, updatedAtUtc, updatedBy: null, data: null)
  {
    Slug = slug;
    Sku = sku;
    Name = name;
    Description = description;
    Price = price;
    CurrencyCode = currencyCode;
    Status = status;
    StockOnHand = stockOnHand;
  }

  public string Slug { get; private set; }
  public string? Sku { get; private set; }
  public string Name { get; private set; }
  public string? Description { get; private set; }
  public decimal Price { get; private set; }
  public string CurrencyCode { get; private set; }
  public ProductStatus Status { get; private set; }
  public int StockOnHand { get; private set; }

  public ProductData? DataJson => GetData<ProductData>();

  public void SetData(ProductData data)
  {
    SetData<ProductData>(data);
  }

  public static Product Create(Guid tenantId,
    string slug,
    string? sku,
    string name,
    string? description,
    decimal price,
    int stockOnHand,
    string currencyCode,
    DateTime createdAtUtc)
  {
    var normalizedSlug = NormalizeSlug(slug);
    var normalizedSku = NormalizeSku(sku);
    EnsureName(name);
    EnsurePrice(price);
    EnsureStock(stockOnHand);
    EnsureCurrency(currencyCode);

    return new Product(Guid.NewGuid(),
      tenantId,
      normalizedSlug,
      normalizedSku,
      name.Trim(),
      NormalizeDescription(description),
      price,
      currencyCode.Trim().ToUpperInvariant(),
      ProductStatus.Draft,
      stockOnHand,
      createdAtUtc,
      createdAtUtc);
  }

  public void UpdateDetails(string name, string? description, decimal price, int stockOnHand, string? sku, DateTime updatedAtUtc)
  {
    EnsureName(name);
    EnsurePrice(price);
    EnsureStock(stockOnHand);
    var normalizedSku = NormalizeSku(sku);

    Name = name.Trim();
    Description = NormalizeDescription(description);
    Price = price;
    StockOnHand = stockOnHand;
    Sku = normalizedSku;
    UpdatedAtUtc = updatedAtUtc;
  }

  public void ChangeStatus(ProductStatus status, DateTime updatedAtUtc)
  {
    Status = status;
    UpdatedAtUtc = updatedAtUtc;
  }

  private static string NormalizeSlug(string slug)
  {
    if (string.IsNullOrWhiteSpace(slug))
      throw new DomainException(ErrorCodes.ValidationError, "El slug del producto es obligatorio.");

    return slug.Trim().ToLowerInvariant();
  }

  private static void EnsureName(string name)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new DomainException(ErrorCodes.ValidationError, "El nombre del producto es obligatorio.");
  }

  private static void EnsurePrice(decimal price)
  {
    if (price < 0)
      throw new DomainException(ErrorCodes.ValidationError, "El precio del producto debe ser mayor o igual a cero.");
  }

  private static void EnsureStock(int stockOnHand)
  {
    if (stockOnHand < 0)
      throw new DomainException(ErrorCodes.ValidationError, "El stock del producto debe ser mayor o igual a cero.");
  }

  private static void EnsureCurrency(string currencyCode)
  {
    if (string.IsNullOrWhiteSpace(currencyCode))
      throw new DomainException(ErrorCodes.ValidationError, "El código de moneda es obligatorio.");
  }

  public static string? NormalizeSku(string? sku)
  {
    if (string.IsNullOrWhiteSpace(sku))
      return null;

    var normalized = sku.Trim().ToUpperInvariant();
    if (normalized.Length > 64)
      throw new DomainException(ErrorCodes.ValidationError, "El SKU del producto no puede exceder 64 caracteres.");

    return normalized;
  }

  private static string? NormalizeDescription(string? description)
    => string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
