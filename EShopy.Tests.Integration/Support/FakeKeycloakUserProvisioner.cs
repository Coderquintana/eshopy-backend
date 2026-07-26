using EShopy.Application.Common.Identity;
using EShopy.Domain.Tenants;

namespace EShopy.Tests.Integration.Support;

/// <summary>Evita llamadas HTTP reales a Keycloak en tests de integracion.</summary>
internal sealed class FakeKeycloakUserProvisioner : IKeycloakUserProvisioner
{
  public Task<string> CreateUserAsync(string email, string name, string subdomain, TenantUserRole role, CancellationToken ct)
    => Task.FromResult($"fake-keycloak-user-{Guid.NewGuid():N}");
}
