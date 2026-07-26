using EShopy.Application.Products;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;
using EShopy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Products;

public sealed class EfProductRepository(EShopyDbContext db) : IProductRepository
{
  public async Task AddAsync(Product product, CancellationToken ct)
  {
    db.Products.Add(product);
    await db.SaveChangesAsync(ct);
  }

  public async Task UpdateAsync(Product product, CancellationToken ct)
  {
    db.Products.Update(product);
    await db.SaveChangesAsync(ct);
  }

  public Task<Product?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    => db.Products.AsNoTracking()
      .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == id, ct);

  public Task<Product?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken ct)
    => db.Products.AsNoTracking()
      .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Slug == slug, ct);

  public async Task<IReadOnlyList<Product>> GetByIdsAsync(Guid tenantId, IReadOnlyCollection<Guid> ids, CancellationToken ct)
  {
    if (ids.Count == 0)
      return [];

    return await db.Products.AsNoTracking()
      .Where(p => p.TenantId == tenantId && ids.Contains(p.Id))
      .ToListAsync(ct);
  }

  public async Task<(IReadOnlyList<Product> Items, long TotalCount)> GetAdminPagedAsync(
    Guid tenantId, PagedQuery query, CancellationToken ct)
  {
    var baseQuery = db.Products.AsNoTracking()
      .Where(p => p.TenantId == tenantId)
      .OrderBy(p => p.Name);

    var total = await baseQuery.LongCountAsync(ct);
    var items = await baseQuery
      .Skip((query.Page - 1) * query.PageSize)
      .Take(query.PageSize)
      .ToListAsync(ct);

    return (items, total);
  }

  public async Task<(IReadOnlyList<Product> Items, long TotalCount)> GetPublicPagedAsync(
    Guid tenantId, PagedQuery query, CancellationToken ct)
  {
    var baseQuery = db.Products.AsNoTracking()
      .Where(p => p.TenantId == tenantId && p.Status == ProductStatus.Active)
      .OrderBy(p => p.Name);

    var total = await baseQuery.LongCountAsync(ct);
    var items = await baseQuery
      .Skip((query.Page - 1) * query.PageSize)
      .Take(query.PageSize)
      .ToListAsync(ct);

    return (items, total);
  }

  public Task<bool> SlugExistsAsync(Guid tenantId, string slug, Guid? excludingId, CancellationToken ct)
    => db.Products.AsNoTracking()
      .AnyAsync(p => p.TenantId == tenantId
        && p.Slug == slug
        && (!excludingId.HasValue || p.Id != excludingId.Value), ct);

  public Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludingId, CancellationToken ct)
    => db.Products.AsNoTracking()
      .AnyAsync(p => p.TenantId == tenantId
        && p.Sku != null
        && p.Sku == sku
        && (!excludingId.HasValue || p.Id != excludingId.Value), ct);
}
