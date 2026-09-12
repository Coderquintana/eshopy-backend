namespace EShopy.Application.Products.Commands;

/// <summary>
/// Lote de altas de producto. Un alta individual es, simplemente, un lote de 1 — no existe
/// un comando ni un endpoint separado para el caso singular (GOVERNANCE.md, "Mutaciones en
/// lote por defecto", D-07).
/// </summary>
public sealed record CreateProductsCommand(IReadOnlyList<CreateProductCommand> Items);
