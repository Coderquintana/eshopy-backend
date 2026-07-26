namespace EShopy.Application.Common.Tenants;

/// <summary>Extrae el subdominio de tenant a partir del host de la request.</summary>
public static class SubdomainResolver
{
  /// <summary>
  /// Ejemplos:
  /// demo.eshopy.com.py =&gt; demo
  /// admin.demo.eshopy.com.py =&gt; demo (admin es prefijo)
  /// localhost =&gt; localhost (dev)
  /// </summary>
  public static string Extract(string host)
  {
    if (string.IsNullOrWhiteSpace(host))
      return "";

    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
      return "localhost";

    var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length < 3) // heuristic
      return parts.FirstOrDefault() ?? "";

    // Si empieza con admin, usar el siguiente como tenant
    if (string.Equals(parts[0], "admin", StringComparison.OrdinalIgnoreCase) && parts.Length >= 2)
      return parts[1];

    return parts[0];
  }
}
