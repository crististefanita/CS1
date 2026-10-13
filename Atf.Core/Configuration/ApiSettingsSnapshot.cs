namespace Atf.Core.Configuration;

using Atf.Core.Configuration.Models;
using Atf.Core.Configuration.Security;

/// <summary>
/// Private execution view of API configuration. The JSON-facing settings stay
/// convenient to bind, while an executor receives a deep copy whose arrays and
/// dictionaries are not shared with the host configuration object.
/// </summary>
internal sealed class ApiSettingsSnapshot
{
    public required string BaseUrl { get; init; }
    public required int TimeoutSeconds { get; init; }
    public required bool WriteTestsEnabled { get; init; }
    public required string TokenEnvironmentVariable { get; init; }
    public required string DefaultAuthProfile { get; init; }
    public required RateLimitSettings RateLimit { get; init; }
    public required PollingSettings Polling { get; init; }
    public required IReadOnlyDictionary<string, AuthProfileSettings> AuthProfiles { get; init; }
    public required RedactionSettings Redaction { get; init; }
    public required RouteSettings Routes { get; init; }
}

internal static class ApiSettingsSnapshotFactory
{
    public static ApiSettingsSnapshot Create(ApiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new ApiSettingsSnapshot
        {
            BaseUrl = settings.BaseUrl,
            TimeoutSeconds = settings.TimeoutSeconds,
            WriteTestsEnabled = settings.WriteTestsEnabled,
            TokenEnvironmentVariable = settings.TokenEnvironmentVariable,
            DefaultAuthProfile = settings.DefaultAuthProfile,
            RateLimit = settings.RateLimit.Snapshot(),
            Polling = settings.Polling.Snapshot(),
            Redaction = settings.Redaction.Snapshot(),
            Routes = settings.Routes.Snapshot(),
            AuthProfiles = settings.AuthProfiles.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Snapshot(),
                StringComparer.OrdinalIgnoreCase)
        };
    }
}
