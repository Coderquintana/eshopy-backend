using System.Net;
using System.Text.Json;
using EShopy.Api.Common.Http;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;

namespace EShopy.Api.Middlewares;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> log)
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task Invoke(HttpContext ctx)
  {
    try
    {
      await next(ctx);
    }
    catch (DomainException ex)
    {
      log.LogWarning(ex, "Domain error: {Code}", ex.Code);
      await WriteError(ctx, HttpStatusCode.Conflict, ex.Code, ex.Message, null);
    }
    catch (Exception ex)
    {
      log.LogError(ex, "Unhandled error");
      await WriteError(ctx, HttpStatusCode.InternalServerError, ErrorCodes.GenericError, "Ha ocurrido un error.", null);
    }
  }

  private static async Task WriteError(HttpContext ctx, HttpStatusCode status, string code, string message, object? details)
  {
    ctx.Response.StatusCode = (int)status;
    ctx.Response.ContentType = "application/json; charset=utf-8";

    var res = new ErrorResponse
    {
      TraceId = ctx.TraceIdentifier,
      Code = code,
      Message = message,
      Details = details
    };

    await ctx.Response.WriteAsync(JsonSerializer.Serialize(res, JsonOptions));
  }
}
