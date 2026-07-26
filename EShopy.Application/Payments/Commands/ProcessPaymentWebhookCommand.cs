namespace EShopy.Application.Payments.Commands;

/// <summary>
/// RawBody y Headers viajan como texto/diccionario, no como HttpRequest — Application no depende de
/// ASP.NET Core (ver IPaymentProviderAdapter). El controller arma este command a partir del request.
/// </summary>
public sealed record ProcessPaymentWebhookCommand(string Provider, string RawBody, IReadOnlyDictionary<string, string> Headers);
