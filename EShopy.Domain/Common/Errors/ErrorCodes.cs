namespace EShopy.Domain.Common.Errors;

public static class ErrorCodes
{
  public const string ValidationError = "VALIDATION_ERROR";
  public const string TenantNotFound = "TENANT_NOT_FOUND";
  public const string Unauthorized = "UNAUTHORIZED";
  public const string Forbidden = "FORBIDDEN";
  public const string NotFound = "NOT_FOUND";
  public const string Conflict = "CONFLICT";
  public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
  public const string GenericError = "GENERIC_ERROR";

  // Catalog / Products
  public const string ProductNotAvailable = "PRODUCT_NOT_AVAILABLE";
  public const string ProductInvalidState = "PRODUCT_INVALID_STATE";

  // Tenants / Subscriptions
  public const string TenantInvalidState = "TENANT_INVALID_STATE";
  public const string TenantSuspended = "TENANT_SUSPENDED";
  public const string TenantCancelled = "TENANT_CANCELLED";

  // Infraestructura / integraciones externas
  public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";
}
