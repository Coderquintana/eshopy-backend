using EShopy.Application.Common.Contracts.Paging;
using EShopy.Application.Products.Contracts;
using EShopy.Application.Products.Requests;

namespace EShopy.Application.Products;

public interface IProductService
{
  Task<ProductAdminDto> CreateAsync(CreateProductRequest request, CancellationToken ct);
  Task<PagedResult<ProductAdminDto>> GetAdminAsync(int page, int pageSize, CancellationToken ct);
  Task<ProductAdminDto> GetByIdAsync(Guid id, CancellationToken ct);
  Task<ProductAdminDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct);
  Task<ProductAdminDto> ChangeStatusAsync(Guid id, ChangeProductStatusRequest request, CancellationToken ct);
  Task<PagedResult<ProductPublicDto>> GetPublicAsync(int page, int pageSize, CancellationToken ct);
  Task<ProductPublicDto> GetBySlugAsync(string slug, CancellationToken ct);
}
