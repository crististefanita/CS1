namespace Atf.Core.Configuration.Models;

public sealed class PollingSettings
{
    public int TimeoutSeconds { get; init; } = 10;
    public int IntervalMilliseconds { get; init; } = 500;

    public PollingSettings Snapshot() => new()
    {
        TimeoutSeconds = TimeoutSeconds,
        IntervalMilliseconds = IntervalMilliseconds
    };
}
