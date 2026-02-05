using EShopy.Application.Products;
using EShopy.Domain.Products;

namespace EShopy.Infrastructure.Products;

public sealed class InMemoryProductRepository : IProductRepository
{
  private readonly Dictionary<Guid, List<Product>> _products = new();
  private readonly object _lock = new();

  public Task AddAsync(Product product, CancellationToken ct)
  {
    lock (_lock)
    {
      if (!_products.TryGetValue(product.TenantId, out var list))
      {
        list = new List<Product>();
        _products[product.TenantId] = list;
      }

      list.Add(product);
    }

    return Task.CompletedTask;
  }

  public Task UpdateAsync(Product product, CancellationToken ct)
  {
    lock (_lock)
    {
      if (!_products.TryGetValue(product.TenantId, out var list))
        return Task.CompletedTask;

      var index = list.FindIndex(p => p.Id == product.Id);
      if (index >= 0)
        list[index] = product;
    }

    return Task.CompletedTask;
  }

  public Task<Product?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct)
  {
    lock (_lock)
    {
      if (_products.TryGetValue(tenantId, out var list))
        return Task.FromResult<Product?>(list.FirstOrDefault(p => p.Id == id));
    }

    return Task.FromResult<Product?>(null);
  }

  public Task<Product?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken ct)
  {
    lock (_lock)
    {
      if (_products.TryGetValue(tenantId, out var list))
        return Task.FromResult<Product?>(list.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase)));
    }

    return Task.FromResult<Product?>(null);
  }

  public Task<IReadOnlyList<Product>> GetAdminListAsync(Guid tenantId, CancellationToken ct)
  {
    lock (_lock)
    {
      if (_products.TryGetValue(tenantId, out var list))
        return Task.FromResult<IReadOnlyList<Product>>(list.ToList());
    }

    return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
  }

  public Task<IReadOnlyList<Product>> GetPublicListAsync(Guid tenantId, CancellationToken ct)
  {
    lock (_lock)
    {
      if (_products.TryGetValue(tenantId, out var list))
      {
        var active = list.Where(p => p.Status == ProductStatus.Active).ToList();
        return Task.FromResult<IReadOnlyList<Product>>(active);
      }
    }

    return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
  }

  public Task<bool> SlugExistsAsync(Guid tenantId, string slug, Guid? excludingId, CancellationToken ct)
  {
    lock (_lock)
    {
      if (_products.TryGetValue(tenantId, out var list))
      {
        var exists = list.Any(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase)
          && (!excludingId.HasValue || p.Id != excludingId.Value));
        return Task.FromResult(exists);
      }
    }

    return Task.FromResult(false);
  }
}
