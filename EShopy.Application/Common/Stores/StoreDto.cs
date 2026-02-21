namespace EShopy.Application.Common.Stores;

/// <summary>Datos mínimos del Store necesarios para el módulo de catálogo.</summary>
public sealed record StoreDto(Guid Id, string CurrencyCode);
