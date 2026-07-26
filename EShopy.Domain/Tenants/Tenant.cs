using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Domain.Tenants;

/// <summary>
/// Entidad global (no multi-tenant): representa al tenant en si, no tiene TenantId.
/// Unica en toda la plataforma por Subdomain.
/// </summary>
public sealed class Tenant
{
  private Tenant(Guid id,
    string subdomain,
    string businessName,
    TenantStatus status,
    TenantPlan plan,
    DateTime createdAtUtc,
    DateTime? updatedAtUtc,
    DateTime? activatedAtUtc)
  {
    Id = id;
    Subdomain = subdomain;
    BusinessName = businessName;
    Status = status;
    Plan = plan;
    CreatedAtUtc = createdAtUtc;
    UpdatedAtUtc = updatedAtUtc;
    ActivatedAtUtc = activatedAtUtc;
  }

  public Guid Id { get; private set; }
  public string Subdomain { get; private set; }
  public string BusinessName { get; private set; }
  public TenantStatus Status { get; private set; }
  public TenantPlan Plan { get; private set; }
  public DateTime CreatedAtUtc { get; private set; }
  public DateTime? UpdatedAtUtc { get; private set; }
  public DateTime? ActivatedAtUtc { get; private set; }

  public static Tenant Create(string subdomain, string businessName, TenantPlan plan, DateTime createdAtUtc)
  {
    var normalizedSubdomain = NormalizeSubdomain(subdomain);
    EnsureBusinessName(businessName);

    return new Tenant(Guid.NewGuid(),
      normalizedSubdomain,
      businessName.Trim(),
      TenantStatus.PendingPayment,
      plan,
      createdAtUtc,
      updatedAtUtc: createdAtUtc,
      activatedAtUtc: null);
  }

  /// <summary>Cambia el estado del tenant validando las transiciones permitidas.</summary>
  /// <remarks>
  /// Transiciones validas:
  /// PendingPayment → Active | Active → Suspended | Suspended → Active
  /// Active → Cancelled | Suspended → Cancelled
  /// </remarks>
  public void ChangeStatus(TenantStatus newStatus, DateTime updatedAtUtc)
  {
    if (Status == newStatus)
      return;

    var allowed = (Status, newStatus) switch
    {
      (TenantStatus.PendingPayment, TenantStatus.Active) => true,
      (TenantStatus.Active, TenantStatus.Suspended) => true,
      (TenantStatus.Suspended, TenantStatus.Active) => true,
      (TenantStatus.Active, TenantStatus.Cancelled) => true,
      (TenantStatus.Suspended, TenantStatus.Cancelled) => true,
      _ => false
    };

    if (!allowed)
      throw new DomainException(
        ErrorCodes.TenantInvalidState,
        $"Transicion de estado no permitida: {Status} → {newStatus}.");

    Status = newStatus;
    UpdatedAtUtc = updatedAtUtc;

    if (newStatus == TenantStatus.Active)
      ActivatedAtUtc ??= updatedAtUtc;
  }

  private static string NormalizeSubdomain(string subdomain)
  {
    if (string.IsNullOrWhiteSpace(subdomain))
      throw new DomainException(ErrorCodes.ValidationError, "El subdominio es obligatorio.");

    var normalized = subdomain.Trim().ToLowerInvariant();

    if (normalized.Length is < 3 or > 50)
      throw new DomainException(ErrorCodes.ValidationError, "El subdominio debe tener entre 3 y 50 caracteres.");

    if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, "^[a-z0-9-]+$"))
      throw new DomainException(ErrorCodes.ValidationError, "El subdominio solo puede contener letras minusculas, numeros y guiones.");

    return normalized;
  }

  private static void EnsureBusinessName(string businessName)
  {
    if (string.IsNullOrWhiteSpace(businessName))
      throw new DomainException(ErrorCodes.ValidationError, "El nombre del negocio es obligatorio.");

    if (businessName.Trim().Length > 200)
      throw new DomainException(ErrorCodes.ValidationError, "El nombre del negocio no puede exceder 200 caracteres.");
  }
}
