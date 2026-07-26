using EShopy.Application.Tenants.Commands;
using EShopy.Application.Tenants.Contracts;
using EShopy.Application.Tenants.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Admin;

/// <summary>Usuarios (Admin/Staff) del tenant resuelto por subdominio. El Owner se crea en el onboarding.</summary>
[Route("api/admin/users")]
[Authorize(Policy = "UsersManage")]
public sealed class UsersController(
  GetTenantUsersQueryHandler getUsersHandler,
  InviteTenantUserCommandHandler inviteHandler) : BaseApiController
{
  /// <summary>Lista los usuarios del tenant actual.</summary>
  [HttpGet]
  [ProducesResponseType(typeof(IReadOnlyList<TenantUserDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<IReadOnlyList<TenantUserDto>>> Get(CancellationToken ct)
  {
    var result = await getUsersHandler.Handle(new GetTenantUsersQuery(), ct);
    return FromResult(result);
  }

  /// <summary>Invita un usuario Admin o Staff al tenant actual (crea el usuario en Keycloak).</summary>
  [HttpPost]
  [ProducesResponseType(typeof(TenantUserDto), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
  [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
  public async Task<ActionResult<TenantUserDto>> Invite([FromBody] InviteTenantUserCommand command, CancellationToken ct)
  {
    var result = await inviteHandler.Handle(command, ct);
    if (!result.IsSuccess)
      return FromResult(result);

    return StatusCode(StatusCodes.Status201Created, result.Value);
  }
}
