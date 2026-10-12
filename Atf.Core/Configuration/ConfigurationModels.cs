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
}

public sealed class RedactionSettings
{
    public bool Enabled { get; init; } = true;
    public string Replacement { get; init; } = "[REDACTED]";
    public string[] SensitiveKeys { get; init; } =
    [
        "access_token", "api_key", "apikey", "authorization", "client_secret",
        "password", "refresh_token", "secret", "token", "email", "email_address",
        "phone", "phone_number"
    ];

    public RedactionSettings Snapshot() => new()
    {
        Enabled = Enabled,
        Replacement = Replacement,
        SensitiveKeys = SensitiveKeys.ToArray()
    };
}

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

public sealed class AuthProfileSettings
{
    public string Type { get; init; } = "none";
    public string? TokenEnvironmentVariable { get; init; }
    public string? HeaderName { get; init; }
    public string? UsernameEnvironmentVariable { get; init; }
    public string? PasswordEnvironmentVariable { get; init; }
    public Dictionary<string, string?> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public AuthProfileSettings Snapshot() => new()
    {
        Type = Type,
        TokenEnvironmentVariable = TokenEnvironmentVariable,
        HeaderName = HeaderName,
        UsernameEnvironmentVariable = UsernameEnvironmentVariable,
        PasswordEnvironmentVariable = PasswordEnvironmentVariable,
        Headers = Headers.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
    };
}

public sealed class RouteSettings
{
    public const string DefaultUsersPath = ApiRoutePaths.UsersCollection;
    public const string DefaultPostsPath = ApiRoutePaths.PostsCollection;
    public const string DefaultUserPostsPath = ApiRoutePaths.UserPostsCollection;

    public string Users { get; init; } = DefaultUsersPath;
    public string Posts { get; init; } = DefaultPostsPath;
    public string UserPosts { get; init; } = DefaultUserPostsPath;

    public RouteSettings Snapshot() => new()
    {
        Users = Users,
        Posts = Posts,
        UserPosts = UserPosts
    };
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

    public ReportingSettings Snapshot() => new()
    {
        Engine = Engine,
        ReportPath = ReportPath,
        TimestampEnabled = TimestampEnabled,
        TimestampFormat = TimestampFormat,
        DocumentTitle = DocumentTitle,
        ReportName = ReportName,
        ConsoleDetails = ConsoleDetails,
        AssertionMaxDisplayCharacters = AssertionMaxDisplayCharacters,
        TestClassOrder = TestClassOrder.ToArray(),
        TestScenarioOrder = TestScenarioOrder.ToArray(),
        TagOrder = TagOrder.ToArray()
    };
}
