using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EShopy.Application.Common.Identity;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace EShopy.Infrastructure.Identity;

/// <summary>
/// Provisiona usuarios en Keycloak via su Admin REST API, reutilizando el service account
/// del client `eshopy-api` (roles realm-management/manage-users, ver docs/keycloak-setup.md).
/// </summary>
public sealed class KeycloakAdminClient(HttpClient httpClient, IConfiguration configuration) : IKeycloakUserProvisioner
{
  private const string OwnerRealmRole = "TENANT_OWNER";

  public async Task<string> CreateOwnerUserAsync(string email, string name, string subdomain, CancellationToken ct)
  {
    var keycloak = configuration.GetSection("Keycloak");
    var adminBaseUrl = (keycloak["AdminBaseUrl"] ?? throw MissingConfig("Keycloak:AdminBaseUrl")).TrimEnd('/');
    var authority = keycloak["Authority"] ?? throw MissingConfig("Keycloak:Authority");
    var realm = ExtractRealm(authority);

    var accessToken = await GetAdminAccessTokenAsync(keycloak, authority, ct);
    var userId = await CreateUserAsync(adminBaseUrl, realm, accessToken, email, name, ct);
    await AssignRealmRoleAsync(adminBaseUrl, realm, accessToken, userId, ct);

    return userId;
  }

  private async Task<string> GetAdminAccessTokenAsync(IConfigurationSection keycloak, string authority, CancellationToken ct)
  {
    var clientId = keycloak["AdminClientId"] ?? throw MissingConfig("Keycloak:AdminClientId");
    var clientSecret = keycloak["AdminClientSecret"] ?? throw MissingConfig("Keycloak:AdminClientSecret");

    using var request = new HttpRequestMessage(HttpMethod.Post, $"{authority}/protocol/openid-connect/token")
    {
      Content = new FormUrlEncodedContent(new Dictionary<string, string>
      {
        ["grant_type"] = "client_credentials",
        ["client_id"] = clientId,
        ["client_secret"] = clientSecret
      })
    };

    using var response = await httpClient.SendAsync(request, ct);
    if (!response.IsSuccessStatusCode)
      throw new DomainException(ErrorCodes.ExternalServiceError, "No se pudo autenticar contra la Keycloak Admin API.");

    var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    return payload.GetProperty("access_token").GetString()
      ?? throw new DomainException(ErrorCodes.ExternalServiceError, "Keycloak no devolvio un access_token valido.");
  }

  private async Task<string> CreateUserAsync(string adminBaseUrl, string realm, string accessToken, string email, string name, CancellationToken ct)
  {
    var (firstName, lastName) = SplitName(name);

    using var request = new HttpRequestMessage(HttpMethod.Post, $"{adminBaseUrl}/admin/realms/{realm}/users");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    request.Content = JsonContent.Create(new
    {
      username = email,
      email,
      firstName,
      lastName,
      enabled = true,
      emailVerified = false,
      credentials = new[]
      {
        new { type = "password", value = Guid.NewGuid().ToString("N"), temporary = true }
      }
    });

    using var response = await httpClient.SendAsync(request, ct);

    if (response.StatusCode == HttpStatusCode.Conflict)
      throw new DomainException(ErrorCodes.Conflict, "Ya existe un usuario en Keycloak con ese email.");

    if (!response.IsSuccessStatusCode)
      throw new DomainException(ErrorCodes.ExternalServiceError, "No se pudo crear el usuario Owner en Keycloak.");

    var location = response.Headers.Location
      ?? throw new DomainException(ErrorCodes.ExternalServiceError, "Keycloak no devolvio la ubicacion del usuario creado.");

    return location.Segments[^1];
  }

  private async Task AssignRealmRoleAsync(string adminBaseUrl, string realm, string accessToken, string userId, CancellationToken ct)
  {
    using var roleRequest = new HttpRequestMessage(HttpMethod.Get, $"{adminBaseUrl}/admin/realms/{realm}/roles/{OwnerRealmRole}");
    roleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    using var roleResponse = await httpClient.SendAsync(roleRequest, ct);
    if (!roleResponse.IsSuccessStatusCode)
      throw new DomainException(ErrorCodes.ExternalServiceError, $"No se pudo obtener el rol de realm '{OwnerRealmRole}'.");

    var role = await roleResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

    using var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"{adminBaseUrl}/admin/realms/{realm}/users/{userId}/role-mappings/realm");
    assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    assignRequest.Content = JsonContent.Create(new[] { role });

    using var assignResponse = await httpClient.SendAsync(assignRequest, ct);
    if (!assignResponse.IsSuccessStatusCode)
      throw new DomainException(ErrorCodes.ExternalServiceError, $"No se pudo asignar el rol '{OwnerRealmRole}' al usuario creado.");
  }

  private static (string FirstName, string LastName) SplitName(string name)
  {
    var parts = name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    return parts.Length == 2 ? (parts[0], parts[1]) : (parts.ElementAtOrDefault(0) ?? name, "");
  }

  private static string ExtractRealm(string authority)
    => authority.TrimEnd('/').Split('/')[^1];

  private static DomainException MissingConfig(string key)
    => new(ErrorCodes.ExternalServiceError, $"Configuracion faltante: '{key}'.");
}
