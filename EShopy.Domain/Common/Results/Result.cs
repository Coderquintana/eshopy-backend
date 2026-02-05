namespace EShopy.Domain.Common.Results;

public readonly record struct Result
{
  public bool IsSuccess { get; }
  public string? Code { get; }
  public string? Message { get; }

  private Result(bool isSuccess, string? code, string? message)
  {
    IsSuccess = isSuccess;
    Code = code;
    Message = message;
  }

  public static Result Ok() => new(true, null, null);
  public static Result Fail(string code, string message) => new(false, code, message);
}

public readonly record struct Result<T>
{
  public bool IsSuccess { get; }
  public T? Value { get; }
  public string? Code { get; }
  public string? Message { get; }

  private Result(bool isSuccess, T? value, string? code, string? message)
  {
    IsSuccess = isSuccess;
    Value = value;
    Code = code;
    Message = message;
  }

  public static Result<T> Ok(T value) => new(true, value, null, null);
  public static Result<T> Fail(string code, string message) => new(false, default, code, message);
}
