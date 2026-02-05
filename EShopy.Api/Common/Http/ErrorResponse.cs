namespace EShopy.Api.Common.Http;

public sealed class ErrorResponse
{
  public required string TraceId { get; init; }
  public required string Code { get; init; }
  public required string Message { get; init; }
  public object? Details { get; init; }
}
