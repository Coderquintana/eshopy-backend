using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;

namespace EShopy.Application.Products;

public interface IProductRepository
{
  Task AddAsync(Product product, CancellationToken ct);
  Task UpdateAsync(Product product, CancellationToken ct);
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
