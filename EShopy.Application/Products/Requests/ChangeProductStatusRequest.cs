using EShopy.Domain.Products;

namespace EShopy.Application.Products.Requests;

public sealed class ChangeProductStatusRequest
{
  public required ProductStatus Status { get; init; }
}
