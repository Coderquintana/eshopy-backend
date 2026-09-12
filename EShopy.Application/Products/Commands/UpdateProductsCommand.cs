namespace EShopy.Application.Products.Commands;

/// <summary>
/// Lote de actualizaciones de producto. Cada item lleva su propio Id y RowVersion — una
/// edición individual es, simplemente, un lote de 1 (GOVERNANCE.md, "Mutaciones en lote por
/// defecto", D-07).
/// </summary>
public sealed record UpdateProductsCommand(IReadOnlyList<UpdateProductCommand> Items);
