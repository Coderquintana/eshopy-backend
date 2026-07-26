using EShopy.Domain.Orders;
using EShopy.Domain.Payments;

namespace EShopy.Application.Orders;

/// <summary>
/// Escribe Order + OrderItems + Payment + el incremento del TenantCounter de OrderNumber en una
/// sola operacion atomica — sin SQL crudo (ver domain/orders.md "Escritura atomica"). El unico otro
/// writer angosto del proyecto ademas de los de Tenants (ver GOVERNANCE.md).
/// </summary>
public interface ICheckoutWriter
{
  /// <summary>
  /// Asigna order.AssignOrderNumber(...) internamente y devuelve el numero generado. No recibe los
  /// OrderItems por separado: ya viajan dentro de order.Items (misma navegacion encapsulada que
  /// Cart.Items), pasarlos aparte seria redundante y una fuente de bugs si alguna vez difieren.
  /// </summary>
  Task<int> CreateAsync(Order order, Payment payment, CancellationToken ct);
}
