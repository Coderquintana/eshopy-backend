using EShopy.Application.Common.Identity;

namespace EShopy.Tests.Integration.Support;

/// <summary>Evita llamadas HTTP reales a Keycloak en tests de integracion.</summary>
internal sealed class FakeKeycloakUserProvisioner : IKeycloakUserProvisioner
{
  public Task<string> CreateOwnerUserAsync(string email, string name, string subdomain, CancellationToken ct)
    => Task.FromResult($"fake-keycloak-user-{Guid.NewGuid():N}");
}
