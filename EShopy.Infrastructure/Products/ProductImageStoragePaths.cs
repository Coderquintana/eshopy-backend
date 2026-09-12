namespace EShopy.Infrastructure.Products;

public static class ProductImageStoragePaths
{
  public const string RequestPath = "/uploads/products";

  public static string ResolveRoot(string contentRootPath)
    => Path.Combine(contentRootPath, "App_Data", "uploads", "products");
}
