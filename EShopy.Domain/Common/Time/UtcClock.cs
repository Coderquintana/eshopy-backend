namespace EShopy.Domain.Common.Time;

public interface IUtcClock
{
  DateTime UtcNow { get; }
}

public sealed class SystemUtcClock : IUtcClock
{
  public DateTime UtcNow => DateTime.UtcNow;
}
