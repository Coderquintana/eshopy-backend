using EShopy.Application.Tenants;

namespace EShopy.Application.Tenants.Commands;

public sealed record DeleteStoreImageCommand(StoreImageKind Kind);
