namespace EShopy.Application.Orders.Queries;

/// <summary>Consulta publica de un pedido. El AccessToken viaja aparte (header), no en el query.</summary>
public sealed record GetPublicOrderQuery(Guid Id);
