using Atf.Core.Configuration;
using Atf.Core.Http;

namespace Atf.Core.Resilience;

public sealed class RateLimitRetryPolicy(RateLimitSettings settings)
{
    public RetryDecision Decide(ApiResponse response, int attempt)
    {
        if (!settings.RetryStatuses.Contains(response.Status))
        {
            return RetryDecision.Complete("HTTP status is not retryable.");
        }

        if (attempt >= settings.MaxRetries)
        {
            return RetryDecision.Complete("Retry budget exhausted.");
        }

        var header = settings.DelayHeaders
            .Select(response.Header)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        var parsedHeaderSeconds = ParseSeconds(header);
        var seconds = parsedHeaderSeconds ?? settings.DefaultDelaySeconds;
        seconds = Math.Clamp(seconds, 1, settings.MaxDelaySeconds);
        return RetryDecision.Retry(
            TimeSpan.FromSeconds(seconds),
            parsedHeaderSeconds is null ? null : header,
            parsedHeaderSeconds is null ? "Configured fallback delay." : "Server retry header delay.");
    }

    private static int? ParseSeconds(string? header)
    {
        return int.TryParse(header?.Trim(), out var seconds) && seconds > 0 ? seconds : null;
    }
}

public sealed record RetryDecision(bool ShouldRetry, TimeSpan Delay, string? Header, string Reason)
{
    public static RetryDecision Retry(TimeSpan delay, string? header, string reason) => new(true, delay, header, reason);
    public static RetryDecision Complete(string reason) => new(false, TimeSpan.Zero, null, reason);
}
