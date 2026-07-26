using EShopy.Application.Tenants.Commands;
using EShopy.Application.Tenants.Contracts;
using EShopy.Application.Tenants.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Admin;

/// <summary>Administracion de tenants a nivel plataforma (SUPERADMIN).</summary>
[Route("api/admin/tenants")]
public sealed class TenantsController(
  GetTenantByIdQueryHandler getByIdHandler,
  ActivateTenantCommandHandler activateHandler) : BaseApiController
{
  /// <summary>Obtiene el detalle de un tenant por ID.</summary>
  [HttpGet("{id:guid}")]
  [Authorize(Policy = "TenantsRead")]
  [ProducesResponseType(typeof(TenantAdminDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  public async Task<ActionResult<TenantAdminDto>> GetById(Guid id, CancellationToken ct)
  {
    var result = await getByIdHandler.Handle(new GetTenantByIdQuery(id), ct);
    return FromResult(result);
  }

  /// <summary>
  /// Activa un tenant en PendingPayment (o lo reactiva desde Suspended). Herramienta de
  /// soporte/ops: hasta que exista el webhook de pago (Fase 8), esta es la unica forma de
  /// activar un tenant.
  /// </summary>
  [HttpPost("{id:guid}/activate")]
  [Authorize(Policy = "TenantsWrite")]
  [ProducesResponseType(typeof(TenantAdminDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
  public async Task<ActionResult<TenantAdminDto>> Activate(Guid id, CancellationToken ct)
  {
    var result = await activateHandler.Handle(new ActivateTenantCommand(id), ct);
    return FromResult(result);
  }
}
