using System.Security.Cryptography;
using EShopy.Domain.Products;

namespace EShopy.Application.Products;

internal static class ProductConcurrency
{
  private const int RowVersionLength = 8;

  internal static string Encode(byte[]? rowVersion)
    => rowVersion is { Length: RowVersionLength }
      ? Convert.ToBase64String(rowVersion)
      : string.Empty;

  internal static bool IsValidToken(string? token)
  {
    if (string.IsNullOrWhiteSpace(token))
      return false;

    try
    {
      return Convert.FromBase64String(token).Length == RowVersionLength;
    }
    catch (FormatException)
    {
      return false;
    }
  }

  internal static byte[] Decode(string token)
    => Convert.FromBase64String(token);

  internal static bool Matches(Product product, byte[] expectedRowVersion)
    => product.RowVersion is { Length: RowVersionLength } currentRowVersion
      && CryptographicOperations.FixedTimeEquals(currentRowVersion, expectedRowVersion);
}
