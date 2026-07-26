namespace EShopy.Application.Tenants.Contracts;

public sealed class TenantUserDto
{
  public required Guid Id { get; init; }
  public required string Email { get; init; }
  public required string Name { get; init; }
  public required string Role { get; init; }
  public required bool IsActive { get; init; }
  public required DateTime CreatedAtUtc { get; init; }
}
