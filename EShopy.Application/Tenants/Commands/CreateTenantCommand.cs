namespace EShopy.Application.Tenants.Commands;

public sealed record CreateTenantCommand(
  string Subdomain,
  string BusinessName,
  string OwnerEmail,
  string OwnerName,
  string Plan);
