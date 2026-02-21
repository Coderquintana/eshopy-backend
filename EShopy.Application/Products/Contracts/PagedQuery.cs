namespace EShopy.Application.Products.Contracts;

/// <summary>Parámetros de paginación para consultas de productos.</summary>
public sealed record PagedQuery(int Page = 1, int PageSize = 20);
