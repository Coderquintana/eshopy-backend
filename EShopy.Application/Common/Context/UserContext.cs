namespace EShopy.Application.Common.Context;

public sealed class UserContext
{
  public string? UserId { get; private set; }
  public string? Email { get; private set; }
  public IReadOnlyCollection<string> Roles { get; private set; } = Array.Empty<string>();

  public void Set(string? userId, string? email, IReadOnlyCollection<string> roles)
  {
    UserId = userId;
    Email = email;
    Roles = roles;
  }
}
