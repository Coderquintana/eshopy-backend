using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EShopy.Tests.Integration.Support;

internal static class TestJwtTokenFactory
{
  internal const string Issuer = "https://test-auth.local/realms/eshopy";
  internal const string Audience = "eshopy-api";
  internal const string SigningKey = "eshopy-tests-signing-key-1234567890";

  public static string CreateToken(
    IEnumerable<string>? permissions = null,
    IEnumerable<string>? roles = null,
    string subject = "test-user-id",
    string email = "test@eshopy.local")
  {
    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, subject),
      new(JwtRegisteredClaimNames.Email, email),
      new("preferred_username", email),
      new("name", "Integration Test User")
    };

    foreach (var permission in permissions ?? Array.Empty<string>())
      claims.Add(new Claim("permissions", permission));

    foreach (var role in roles ?? Array.Empty<string>())
      claims.Add(new Claim("roles", role));

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
      issuer: Issuer,
      audience: Audience,
      claims: claims,
      notBefore: DateTime.UtcNow.AddMinutes(-1),
      expires: DateTime.UtcNow.AddHours(1),
      signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
