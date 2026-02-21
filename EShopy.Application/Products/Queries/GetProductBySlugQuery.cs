namespace EShopy.Application.Products.Queries;

/// <summary>Consulta pública para obtener un producto por su slug (solo productos Active).</summary>
public sealed record GetProductBySlugQuery(string Slug);
