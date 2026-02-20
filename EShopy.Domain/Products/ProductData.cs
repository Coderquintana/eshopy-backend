namespace EShopy.Domain.Products;

public sealed record ProductData(
  string? AdditionalInfo,
  Dictionary<string, string>? Attributes,
  string? ExternalReference);
