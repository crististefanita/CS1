namespace Atf.Core.Configuration.Models;

using Atf.Core.Configuration;

public sealed class AtfModuleSettings
{
    public AtfModuleConfig Config { get; init; } = new();
    public ApiSettings Api => Config.Api;
    public ReportingSettings Reporting => Config.Reporting;
}

public sealed class AtfModuleConfig
{
    public ApiSettings Api { get; init; } = new();
    public ReportingSettings Reporting { get; init; } = new();
}

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
    public RouteSettings Routes { get; init; } = new();
}

public sealed class RateLimitSettings
{
    public int MaxRetries { get; init; } = 2;
    public int DefaultDelaySeconds { get; init; } = 5;
    public int MaxDelaySeconds { get; init; } = 65;
    public int[] RetryStatuses { get; init; } = [429];
    public string[] DelayHeaders { get; init; } = ["Retry-After", "x-ratelimit-reset"];
}

public sealed class PollingSettings
{
    public int TimeoutSeconds { get; init; } = 10;
    public int IntervalMilliseconds { get; init; } = 500;
}

public sealed class AuthProfileSettings
{
    public string Type { get; init; } = "none";
    public string? TokenEnvironmentVariable { get; init; }
    public string? HeaderName { get; init; }
}

public sealed class RouteSettings
{
    public const string DefaultUsersPath = ApiRoutePaths.UsersCollection;
    public const string DefaultPostsPath = ApiRoutePaths.PostsCollection;
    public const string DefaultUserPostsPath = ApiRoutePaths.UserPostsCollection;

    public string Users { get; init; } = DefaultUsersPath;
    public string Posts { get; init; } = DefaultPostsPath;
    public string UserPosts { get; init; } = DefaultUserPostsPath;
}

public sealed class ReportingSettings
{
    public string Engine { get; init; } = "extent";
    public string ReportPath { get; init; } = "artifacts/reports/api-atf-report.html";
    public bool TimestampEnabled { get; init; } = true;
    public string TimestampFormat { get; init; } = "yyyy-MM-dd_HH-mm-ss";
    public string DocumentTitle { get; init; } = "ATF API — Extent report";
    public string ReportName { get; init; } = "Configurable API Automation";
    public bool ConsoleDetails { get; init; } = true;
    public int AssertionMaxDisplayCharacters { get; init; } = ReportingDefaults.AssertionMaxDisplayCharacters;

    /// <summary>
    /// Optional deterministic order for the native Extent test navigation.
    /// Entries not listed here retain their original order after the configured entries.
    /// </summary>
    public string[] TestClassOrder { get; init; } = [];

    /// <summary>
    /// Optional deterministic order for scenario cards inside a test class.
    /// </summary>
    public string[] TestScenarioOrder { get; init; } = [];

    /// <summary>
    /// Optional deterministic order for the tag pills shown in test details.
    /// </summary>
    public string[] TagOrder { get; init; } = [];
}
