using EShopy.Domain.Tenants;

namespace EShopy.Application.Common.Tenants;

public sealed record TenantResolution(Guid TenantId, TenantStatus Status);
