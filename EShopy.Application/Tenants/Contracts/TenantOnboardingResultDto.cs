namespace EShopy.Application.Tenants.Contracts;

public sealed class TenantOnboardingResultDto
{
  public required Guid TenantId { get; init; }
  public required string Subdomain { get; init; }
  public required string Status { get; init; }
}
