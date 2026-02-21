namespace EShopy.Application.Products.Commands;

/// <summary>Comando para actualizar los campos editables de un producto.</summary>
public sealed record UpdateProductCommand(
  Guid Id,
  string Name,
  string? Description,
  decimal Price,
  int StockOnHand,
  string? Sku);
