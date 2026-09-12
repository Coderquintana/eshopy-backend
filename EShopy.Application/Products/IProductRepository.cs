using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;

namespace EShopy.Application.Products;

public interface IProductRepository
{
  /// <summary>Alta singular — usada por operaciones que crean un solo producto fuera del lote (ninguna hoy; ver `AddRangeAsync`).</summary>
  Task AddAsync(Product product, CancellationToken ct);

  /// <summary>Actualización singular — usada por operaciones intrínsecamente unitarias (subir una imagen, cambiar un estado puntual).</summary>
  Task UpdateAsync(Product product, byte[] expectedRowVersion, CancellationToken ct);

  /// <summary>Alta en lote: todo el lote se persiste en una sola transacción (D-07, GOVERNANCE.md "Mutaciones en lote por defecto").</summary>
  Task AddRangeAsync(IReadOnlyList<Product> products, CancellationToken ct);

  /// <summary>Actualización en lote: todo el lote se persiste en una sola transacción (D-07). Si el RowVersion de un solo item no coincide, no se persiste ninguno.</summary>
  Task UpdateRangeAsync(IReadOnlyList<(Product Product, byte[] ExpectedRowVersion)> items, CancellationToken ct);

  Task<Product?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct);
  Task<Product?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken ct);

  /// <summary>Batch lookup — usado por Cart para armar su DTO sin N+1 queries.</summary>
  Task<IReadOnlyList<Product>> GetByIdsAsync(Guid tenantId, IReadOnlyCollection<Guid> ids, CancellationToken ct);

  /// <summary>Lista paginada para el panel de administración (todos los estados).</summary>
  Task<(IReadOnlyList<Product> Items, long TotalCount)> GetAdminPagedAsync(Guid tenantId, PagedQuery query, CancellationToken ct);

  /// <summary>Lista paginada pública (solo productos Active).</summary>
  Task<(IReadOnlyList<Product> Items, long TotalCount)> GetPublicPagedAsync(Guid tenantId, PagedQuery query, CancellationToken ct);

  Task<bool> SlugExistsAsync(Guid tenantId, string slug, Guid? excludingId, CancellationToken ct);
  Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludingId, CancellationToken ct);
}
