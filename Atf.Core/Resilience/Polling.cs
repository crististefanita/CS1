using System.Diagnostics;

namespace Atf.Core.Resilience;

public sealed record PollingResult<T>(T LastValue, int Attempts, TimeSpan Elapsed, bool Satisfied);

public static class Polling
{
    public static async Task<PollingResult<T>> UntilAsync<T>(
        Func<CancellationToken, Task<T>> supplier,
        Func<T, bool> condition,
        TimeSpan timeout,
        TimeSpan interval,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        ArgumentNullException.ThrowIfNull(condition);
        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Polling timeout cannot be negative.");
        }

        if (interval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "Polling interval cannot be negative.");
        }

        var started = Stopwatch.GetTimestamp();
        var attempts = 0;
        T? last = default;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            last = await supplier(cancellationToken);
            attempts++;
            if (condition(last))
            {
                return new(last, attempts, Stopwatch.GetElapsedTime(started), true);
            }

            var elapsed = Stopwatch.GetElapsedTime(started);
            if (elapsed >= timeout || timeout - elapsed <= interval)
            {
                break;
            }

            await Task.Delay(interval, cancellationToken);
        } while (Stopwatch.GetElapsedTime(started) < timeout);

        return new(last!, attempts, Stopwatch.GetElapsedTime(started), false);
    }
}
