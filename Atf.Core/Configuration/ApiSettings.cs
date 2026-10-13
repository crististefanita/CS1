namespace Atf.Core.Configuration.Models;

using Atf.Core.Configuration;
using Atf.Core.Configuration.Security;

public sealed class ApiSettings
{
    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public bool WriteTestsEnabled { get; init; } = true;
    public string TokenEnvironmentVariable { get; init; } = "ATF_API_TOKEN";
    public string DefaultAuthProfile { get; init; } = "default";
    public RateLimitSettings RateLimit { get; init; } = new();
    public PollingSettings Polling { get; init; } = new();
    public Dictionary<string, AuthProfileSettings> AuthProfiles { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public RedactionSettings Redaction { get; init; } = new();
    public RouteSettings Routes { get; init; } = new();

    /// <summary>
    /// Captures the values used by an executor so later host/configuration
    /// mutations cannot change an in-flight test run.
    /// </summary>
    public ApiSettings Snapshot() => new()
    {
        BaseUrl = BaseUrl,
        TimeoutSeconds = TimeoutSeconds,
        WriteTestsEnabled = WriteTestsEnabled,
        TokenEnvironmentVariable = TokenEnvironmentVariable,
        DefaultAuthProfile = DefaultAuthProfile,
        RateLimit = RateLimit.Snapshot(),
        Polling = Polling.Snapshot(),
        Redaction = Redaction.Snapshot(),
        Routes = Routes.Snapshot(),
        AuthProfiles = AuthProfiles.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Snapshot(),
            StringComparer.OrdinalIgnoreCase)
    };

    internal ApiSettingsSnapshot ToRuntimeSnapshot() => ApiSettingsSnapshotFactory.Create(this);
}
