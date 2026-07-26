using EShopy.Application.Products.Contracts;

namespace EShopy.Application.Orders.Queries;

/// <summary>Consulta para listar pedidos del panel admin con paginación.</summary>
public sealed record GetOrdersQuery(PagedQuery Paging);
