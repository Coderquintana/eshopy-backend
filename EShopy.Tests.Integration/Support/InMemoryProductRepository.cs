using EShopy.Application.Products;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Products;

namespace EShopy.Tests.Integration.Support;

internal sealed class InMemoryProductRepository : IProductRepository
{
  private readonly object _sync = new();
  private readonly Dictionary<Guid, List<Product>> _productsByTenant = new();

  public Task AddAsync(Product product, CancellationToken ct)
  {
    lock (_sync)
    {
      if (!_productsByTenant.TryGetValue(product.TenantId, out var products))
      {
        products = [];
        _productsByTenant[product.TenantId] = products;
      }

      products.Add(product);
    }

    return Task.CompletedTask;
  }

  public Task UpdateAsync(Product product, CancellationToken ct) => Task.CompletedTask;

  public Task<Product?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct)
  {
    lock (_sync)
    {
      var product = _productsByTenant.TryGetValue(tenantId, out var products)
        ? products.FirstOrDefault(x => x.Id == id)
        : null;

      return Task.FromResult(product);
    }
  }

  public Task<Product?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken ct)
  {
    lock (_sync)
    {
      var product = _productsByTenant.TryGetValue(tenantId, out var products)
        ? products.FirstOrDefault(x => x.Slug == slug)
        : null;

      return Task.FromResult(product);
    }
  }

  public Task<(IReadOnlyList<Product> Items, long TotalCount)> GetAdminPagedAsync(Guid tenantId, PagedQuery query, CancellationToken ct)
  {
    lock (_sync)
    {
      var products = _productsByTenant.TryGetValue(tenantId, out var items)
        ? items.AsEnumerable()
        : Enumerable.Empty<Product>();

      var totalCount = products.LongCount();
      var pageItems = products
        .OrderBy(x => x.CreatedAtUtc)
        .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize)
        .ToList();

      return Task.FromResult(((IReadOnlyList<Product>)pageItems, totalCount));
    }
  }

  public Task<(IReadOnlyList<Product> Items, long TotalCount)> GetPublicPagedAsync(Guid tenantId, PagedQuery query, CancellationToken ct)
  {
    lock (_sync)
    {
      var products = _productsByTenant.TryGetValue(tenantId, out var items)
        ? items.Where(x => x.Status == ProductStatus.Active)
        : Enumerable.Empty<Product>();

      var totalCount = products.LongCount();
      var pageItems = products
        .OrderBy(x => x.CreatedAtUtc)
        .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize)
        .ToList();

      return Task.FromResult(((IReadOnlyList<Product>)pageItems, totalCount));
    }
  }

  public Task<bool> SlugExistsAsync(Guid tenantId, string slug, Guid? excludingId, CancellationToken ct)
  {
    lock (_sync)
    {
      var exists = _productsByTenant.TryGetValue(tenantId, out var products) &&
        products.Any(x => x.Slug == slug && (!excludingId.HasValue || x.Id != excludingId.Value));

      return Task.FromResult(exists);
    }
  }

  public Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludingId, CancellationToken ct)
  {
    lock (_sync)
    {
      var exists = _productsByTenant.TryGetValue(tenantId, out var products) &&
        products.Any(x =>
          string.Equals(x.Sku, sku, StringComparison.OrdinalIgnoreCase) &&
          (!excludingId.HasValue || x.Id != excludingId.Value));

      return Task.FromResult(exists);
    }
  }
}
