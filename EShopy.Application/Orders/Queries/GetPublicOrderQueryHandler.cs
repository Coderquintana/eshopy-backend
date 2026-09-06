using EShopy.Application.Common.Context;
using EShopy.Application.Orders.Contracts;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Results;

namespace EShopy.Application.Orders.Queries;

/// <summary>
/// Autoriza por posesion del AccessToken devuelto en el checkout, no por sesion (el comprador es
/// anonimo). Un token que no coincide responde NotFound y no Forbidden: distinguirlos permitiria
/// enumerar que pedidos existen en el tenant.
/// </summary>
public sealed class GetPublicOrderQueryHandler(
  IOrderRepository repository,
  TenantContext tenantContext)
{
  public async Task<Result<PublicOrderDto>> Handle(GetPublicOrderQuery query, string accessToken, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(accessToken))
      return Result<PublicOrderDto>.Fail(ErrorCodes.ValidationError, "El header X-Order-Token es obligatorio.");

    if (!tenantContext.TenantId.HasValue)
      return Result<PublicOrderDto>.Fail(ErrorCodes.TenantNotFound, "No se pudo resolver el tenant.");

    var order = await repository.GetByIdAsync(tenantContext.TenantId.Value, query.Id, ct);
    if (order is null || !order.MatchesAccessToken(accessToken))
      return Result<PublicOrderDto>.Fail(ErrorCodes.NotFound, "Pedido no encontrado.");

    return Result<PublicOrderDto>.Ok(OrderMappings.ToPublicDto(order));
  }
}
