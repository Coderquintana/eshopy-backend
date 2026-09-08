using EShopy.Application.Common.Context;
using EShopy.Application.Tenants;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Infrastructure.Identity;

namespace EShopy.Api.Middlewares;

/// <summary>
/// Impide que un usuario autenticado opere sobre un tenant al que no pertenece.
/// Los permisos determinan que puede hacer; la membresia determina en que tenant puede hacerlo.
/// </summary>
public sealed class TenantMembershipMiddleware(RequestDelegate next)
{
  public async Task Invoke(
    HttpContext ctx,
    TenantContext tenantContext,
    UserContextAccessor userContextAccessor,
    ITenantUserRepository tenantUserRepository)
  {
    if (ctx.User.Identity?.IsAuthenticated != true || tenantContext.TenantId is null)
    {
      await next(ctx);
      return;
    }

    var user = userContextAccessor.GetUserContext();
    if (user.IsSuperAdmin)
    {
      await next(ctx);
      return;
    }

    var tenantUser = await tenantUserRepository.GetByKeycloakUserIdAsync(
      tenantContext.TenantId.Value,
      user.UserId,
      ctx.RequestAborted);

    if (tenantUser is null)
      throw new DomainException(ErrorCodes.Forbidden, "No tenés acceso a esta tienda.");

    await next(ctx);
  }
}
