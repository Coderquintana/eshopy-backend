using EShopy.Application.Carts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EShopy.Infrastructure.Carts;

/// <summary>
/// F6-04: borra periodicamente los carritos vencidos de todos los tenants. Corre en su propio
/// scope de DI (ICartRepository/EShopyDbContext son Scoped, este servicio es Singleton) y sin
/// TenantId fijado — el Global Query Filter queda transparente, igual que el webhook de pagos.
/// </summary>
public sealed class CartCleanupBackgroundService(
  IServiceScopeFactory scopeFactory,
  IConfiguration configuration,
  ILogger<CartCleanupBackgroundService> log) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var intervalMinutes = configuration.GetValue("CartCleanup:IntervalMinutes", 60);
    var interval = TimeSpan.FromMinutes(intervalMinutes);

    while (!stoppingToken.IsCancellationRequested)
    {
      await CleanupExpiredCartsAsync(stoppingToken);

      try
      {
        await Task.Delay(interval, stoppingToken);
      }
      catch (OperationCanceledException)
      {
        break;
      }
    }
  }

  private async Task CleanupExpiredCartsAsync(CancellationToken ct)
  {
    try
    {
      using var scope = scopeFactory.CreateScope();
      var cartRepository = scope.ServiceProvider.GetRequiredService<ICartRepository>();

      var deletedCount = await cartRepository.DeleteExpiredAsync(DateTime.UtcNow, ct);
      if (deletedCount > 0)
        log.LogInformation("Cart cleanup: {DeletedCount} carritos expirados eliminados", deletedCount);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      // No debe tumbar el proceso: se reintenta en el proximo ciclo.
      log.LogError(ex, "Cart cleanup fallo, se reintentara en el proximo ciclo");
    }
  }
}
