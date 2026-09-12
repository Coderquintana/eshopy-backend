namespace EShopy.Infrastructure.Tenants;

public static class StoreImageStoragePaths
{
  public const string RequestPath = "/uploads/stores";

  public static string ResolveRoot(string contentRootPath)
    => Path.Combine(contentRootPath, "App_Data", "uploads", "stores");
}
