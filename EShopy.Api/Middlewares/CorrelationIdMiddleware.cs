namespace EShopy.Api.Middlewares;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
  private const string HeaderName = "X-Correlation-Id";

  public async Task Invoke(HttpContext ctx)
  {
    var correlationId = ctx.Request.Headers.TryGetValue(HeaderName, out var value) && !string.IsNullOrWhiteSpace(value)
      ? value.ToString()
      : Guid.NewGuid().ToString("N");

    ctx.Items[HeaderName] = correlationId;
    ctx.Response.Headers[HeaderName] = correlationId;

    await next(ctx);
  }
}
