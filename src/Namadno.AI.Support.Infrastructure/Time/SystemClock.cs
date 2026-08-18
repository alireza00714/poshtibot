using Namadno.AI.Support.Application.Abstractions.Time;

namespace Namadno.AI.Support.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
