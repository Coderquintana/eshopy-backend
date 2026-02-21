using EShopy.Domain.Products;

namespace EShopy.Application.Products.Commands;

/// <summary>Comando para cambiar el estado de un producto.</summary>
public sealed record ChangeProductStatusCommand(Guid Id, ProductStatus Status);
