using EShopy.Application.Orders;
using EShopy.Domain.Common.Counters;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using EShopy.Domain.Orders;
using EShopy.Domain.Payments;
using EShopy.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Orders;

/// <summary>
/// Genera OrderNumber atomicamente (TenantCounter.CurrentValue como concurrency token EF, sin SQL
/// crudo) y persiste Order+OrderItems+Payment en el mismo SaveChangesAsync. Ver domain/orders.md
/// "Escritura atomica" para el razonamiento completo.
/// </summary>
public sealed class EfCheckoutWriter(EShopyDbContext db) : ICheckoutWriter
{
  private const int MaxRetries = 5;

  public async Task<int> CreateAsync(Order order, Payment payment, CancellationToken ct)
  {
    for (var attempt = 0; attempt < MaxRetries; attempt++)
    {
      var counter = await GetOrCreateCounterAsync(order.TenantId, ct);
      counter.Increment();
      order.AssignOrderNumber(counter.CurrentValue);

      // db.Orders.Add(order) ya cascadea a order.Items (navegacion EF real, igual que Cart.Items) —
      // no hace falta agregar los OrderItems por separado.
      db.Orders.Add(order);
      db.Payments.Add(payment);

      try
      {
        await db.SaveChangesAsync(ct);
        return counter.CurrentValue;
      }
      catch (DbUpdateConcurrencyException)
      {
        // Otro checkout leyo el mismo CurrentValue y gano la carrera — descartar todo lo trackeado
        // en este intento (Order/Payment/counter) y reintentar desde cero.
        db.ChangeTracker.Clear();
      }
      catch (DbUpdateException ex) when (IsOrderNumberRaceViolation(ex))
      {
        // Bajo contencion real el UPDATE del counter puede quedar bloqueado por otra transaccion en
        // vuelo; para cuando se desbloquea, su WHERE ya no matchea (0 filas) pero SQL Server no aborta
        // el resto del batch por eso — el INSERT de Order con el mismo OrderNumber que acaba de
        // confirmar el ganador sigue ejecutandose y choca contra UQ_Orders_TenantId_OrderNumber. Es la
        // misma condicion de carrera que DbUpdateConcurrencyException, solo que se manifiesta como una
        // violacion de constraint en vez de un mismatch de filas afectadas — mismo tratamiento: reintentar.
        db.ChangeTracker.Clear();
      }
    }

    throw new DomainException(ErrorCodes.ConcurrencyConflict, "No se pudo generar el numero de pedido tras varios intentos.");
  }

  private static readonly int[] UniqueConstraintViolationErrorNumbers = [2601, 2627];

  private static bool IsOrderNumberRaceViolation(DbUpdateException ex)
    => ex.InnerException is SqlException sqlEx && UniqueConstraintViolationErrorNumbers.Contains(sqlEx.Number);

  private async Task<TenantCounter> GetOrCreateCounterAsync(Guid tenantId, CancellationToken ct)
  {
    var counter = await db.TenantCounters
      .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CounterType == TenantCounter.OrderNumberType, ct);

    if (counter is not null)
      return counter;

    counter = TenantCounter.Create(tenantId, TenantCounter.OrderNumberType);
    db.TenantCounters.Add(counter);
    return counter;
  }
}
