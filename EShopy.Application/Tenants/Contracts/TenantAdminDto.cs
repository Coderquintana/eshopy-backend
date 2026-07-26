namespace EShopy.Application.Tenants.Contracts;

public sealed class TenantAdminDto
{
  public required Guid Id { get; init; }
  public required string Subdomain { get; init; }
  public required string BusinessName { get; init; }
  public required string Status { get; init; }
  public required string Plan { get; init; }
  public required DateTime CreatedAtUtc { get; init; }
  public DateTime? ActivatedAtUtc { get; init; }
}
