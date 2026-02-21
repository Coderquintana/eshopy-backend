namespace EShopy.Application.Products.Queries;

/// <summary>Consulta para obtener el detalle de un producto por ID (panel admin).</summary>
public sealed record GetProductByIdQuery(Guid Id);
