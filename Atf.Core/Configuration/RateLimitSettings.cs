namespace Atf.Core.Configuration.Models;

public sealed class RateLimitSettings
{
    public int MaxRetries { get; init; } = 2;
    public int DefaultDelaySeconds { get; init; } = 5;
    public int MaxDelaySeconds { get; init; } = 65;
    public int[] RetryStatuses { get; init; } = [429];
    public string[] DelayHeaders { get; init; } = ["Retry-After", "x-ratelimit-reset"];

    public RateLimitSettings Snapshot() => new()
    {
        MaxRetries = MaxRetries,
        DefaultDelaySeconds = DefaultDelaySeconds,
        MaxDelaySeconds = MaxDelaySeconds,
        RetryStatuses = RetryStatuses.ToArray(),
        DelayHeaders = DelayHeaders.ToArray()
    };
}
