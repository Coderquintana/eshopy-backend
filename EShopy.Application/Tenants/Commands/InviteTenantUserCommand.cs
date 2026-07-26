namespace EShopy.Application.Tenants.Commands;

public sealed record InviteTenantUserCommand(string Email, string Name, string Role);
