namespace Namadno.AI.Support.Application.Abstractions.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
