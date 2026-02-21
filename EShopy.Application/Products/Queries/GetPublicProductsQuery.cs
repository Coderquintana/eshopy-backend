using EShopy.Application.Products.Contracts;

namespace EShopy.Application.Products.Queries;

/// <summary>Consulta para listar productos activos en el storefront público.</summary>
public sealed record GetPublicProductsQuery(PagedQuery Paging);
