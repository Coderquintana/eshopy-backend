using EShopy.Application.Tenants.Commands;
using EShopy.Application.Tenants.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

/// <summary>Onboarding de tenants nuevos: crea el comercio en estado PendingPayment.</summary>
[AllowAnonymous]
[Route("api/onboarding/tenants")]
public sealed class OnboardingController(CreateTenantCommandHandler createHandler) : BaseApiController
{
  /// <summary>Crea un tenant nuevo (Tenant + Store + Owner en Keycloak + Subscription pendiente).</summary>
  [HttpPost]
  [ProducesResponseType(typeof(TenantOnboardingResultDto), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
  [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
  public async Task<ActionResult<TenantOnboardingResultDto>> Create([FromBody] CreateTenantCommand command, CancellationToken ct)
  {
    var result = await createHandler.Handle(command, ct);
    if (!result.IsSuccess)
      return FromResult(result);

    return StatusCode(StatusCodes.Status201Created, result.Value);
  }
}
