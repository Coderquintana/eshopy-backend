using EShopy.Application.Common.Context;
using Microsoft.AspNetCore.Mvc;

namespace EShopy.Api.Controllers.Public;

[Route("api/store")]
public sealed class StoreController(TenantContext tenant) : BaseController
{
  [HttpGet]
  [ProducesResponseType(typeof(StorePublicDto), StatusCodes.Status200OK)]
  public ActionResult<StorePublicDto> GetStore()
  {
    // Skeleton: devolver valores mínimos. En implementación real: leer desde DB.
    return Ok(new StorePublicDto
    {
      StoreId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
      Name = $"eShopy ({tenant.Subdomain})",
      CurrencyCode = "PYG",
      Timezone = "America/Asuncion",
      PrimaryColor = null,
      LogoUrl = null
    });
  }
}

public sealed class StorePublicDto
{
  public required Guid StoreId { get; init; }
  public required string Name { get; init; }
  public required string CurrencyCode { get; init; }
  public required string Timezone { get; init; }
  public string? PrimaryColor { get; init; }
  public string? LogoUrl { get; init; }
}
