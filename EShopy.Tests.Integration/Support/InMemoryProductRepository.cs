using System.Reflection;
using EShopy.Application.Products;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Common.Entities;
using EShopy.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Tests.Integration.Support;

internal sealed class InMemoryProductRepository : IProductRepository
{
  private static readonly MethodInfo RowVersionSetter =
    typeof(AppEntity).GetProperty(nameof(AppEntity.RowVersion))!.GetSetMethod(nonPublic: true)!;

  private readonly object _sync = new();
  private readonly Dictionary<Guid, List<Product>> _productsByTenant = new();
  private long _rowVersionCounter;

  public Task AddAsync(Product product, CancellationToken ct)
  {
    lock (_sync)
    {
      if (!_productsByTenant.TryGetValue(product.TenantId, out var products))
      {
        products = [];
        _productsByTenant[product.TenantId] = products;
      }

      AdvanceRowVersion(product);
      products.Add(product);
    }

    return Task.CompletedTask;
  }

  public Task UpdateAsync(Product product, byte[] expectedRowVersion, CancellationToken ct)
  {
    lock (_sync)
    {
      if (product.RowVersion is not { Length: 8 } currentRowVersion ||
          !currentRowVersion.AsSpan().SequenceEqual(expectedRowVersion))
      {
        throw new DbUpdateConcurrencyException("El RowVersion esperado no coincide con el actual.");
      }

      AdvanceRowVersion(product);
    }

    return Task.CompletedTask;
  }

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

  public Task<IReadOnlyList<Product>> GetByIdsAsync(Guid tenantId, IReadOnlyCollection<Guid> ids, CancellationToken ct)
  {
    lock (_sync)
    {
      var products = _productsByTenant.TryGetValue(tenantId, out var items)
        ? items.Where(x => ids.Contains(x.Id)).ToList()
        : [];

      return Task.FromResult((IReadOnlyList<Product>)products);
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

  private void AdvanceRowVersion(Product product)
  {
    var rowVersion = BitConverter.GetBytes(Interlocked.Increment(ref _rowVersionCounter));
    RowVersionSetter.Invoke(product, new object?[] { rowVersion });
  }
}
