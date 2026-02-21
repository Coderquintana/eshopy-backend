using EShopy.Application.Products.Contracts;

namespace EShopy.Application.Products.Queries;

/// <summary>Consulta para listar productos del panel admin con paginación.</summary>
public sealed record GetProductsQuery(PagedQuery Paging);
