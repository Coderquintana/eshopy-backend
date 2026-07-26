using System.Net;
using System.Text.Json;
using EShopy.Api.Common.Http;
using EShopy.Domain.Common.Errors;
using EShopy.Domain.Common.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Api.Middlewares;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> log)
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  // Numeros de error de SQL Server para violacion de constraint unico (2601: indice unico, 2627: PK/UNIQUE).
  private static readonly int[] UniqueConstraintViolationErrorNumbers = [2601, 2627];

  public async Task Invoke(HttpContext ctx)
  {
    try
    {
      await next(ctx);
    }
    catch (DomainException ex)
    {
      log.LogWarning(ex, "Domain error: {Code}", ex.Code);
      await WriteError(ctx, MapStatus(ex.Code), ex.Code, ex.Message, null);
    }
    catch (DbUpdateConcurrencyException ex)
    {
      log.LogWarning(ex, "Concurrency conflict");
      await WriteError(ctx, HttpStatusCode.Conflict, ErrorCodes.ConcurrencyConflict,
        "El recurso fue modificado por otro proceso. Vuelva a intentarlo.", null);
    }
    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
    {
      log.LogWarning(ex, "Unique constraint violation");
      await WriteError(ctx, HttpStatusCode.Conflict, ErrorCodes.Conflict,
        "Ya existe un registro con esos datos.", null);
    }
    catch (Exception ex)
    {
      log.LogError(ex, "Unhandled error");
      await WriteError(ctx, HttpStatusCode.InternalServerError, ErrorCodes.GenericError, "Ha ocurrido un error.", null);
    }
  }

  private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    => ex.InnerException is SqlException sqlEx
      && UniqueConstraintViolationErrorNumbers.Contains(sqlEx.Number);

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

  private static HttpStatusCode MapStatus(string code)
    => code switch
    {
      ErrorCodes.ValidationError => HttpStatusCode.BadRequest,
      ErrorCodes.TenantNotFound => HttpStatusCode.NotFound,
      ErrorCodes.NotFound => HttpStatusCode.NotFound,
      ErrorCodes.Unauthorized => HttpStatusCode.Unauthorized,
      ErrorCodes.Forbidden => HttpStatusCode.Forbidden,
      ErrorCodes.Conflict => HttpStatusCode.Conflict,
      ErrorCodes.ProductInvalidState => HttpStatusCode.Conflict,
      ErrorCodes.ProductNotAvailable => HttpStatusCode.Conflict,
      _ => HttpStatusCode.InternalServerError
    };
}
