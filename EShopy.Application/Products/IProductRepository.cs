using EShopy.Domain.Products;

namespace EShopy.Application.Products;

public interface IProductRepository
{
  Task AddAsync(Product product, CancellationToken ct);
  Task UpdateAsync(Product product, CancellationToken ct);
  Task<Product?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct);
  Task<Product?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken ct);
  Task<IReadOnlyList<Product>> GetAdminListAsync(Guid tenantId, CancellationToken ct);
  Task<IReadOnlyList<Product>> GetPublicListAsync(Guid tenantId, CancellationToken ct);
  Task<bool> SlugExistsAsync(Guid tenantId, string slug, Guid? excludingId, CancellationToken ct);
}
