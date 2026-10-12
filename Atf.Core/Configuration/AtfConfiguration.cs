using Atf.Core.Reporting;

namespace Atf.Core.Configuration;

public sealed class AtfConfiguration
{
    public Dictionary<string, AtfModuleSettings> Modules { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Compatibility facades keep the API and reporting layers independent from the
    // physical configuration shape while making Modules:Api the single ownership boundary.
    public ApiSettings Api => GetModule("api").Api;
    public ReportingSettings Reporting => GetModule("api").Reporting;

    public static AtfConfiguration Load(string? basePath = null)
        => new AtfConfigurationLoader().Load(basePath);

    public void Validate()
    {
        if (!Modules.ContainsKey("api"))
        {
            throw new InvalidOperationException("Modules:Api configuration is required.");
        }

        if (string.IsNullOrWhiteSpace(Api.BaseUrl))
        {
            throw new InvalidOperationException("Modules:Api:Api:BaseUrl is required.");
        }

        if (!Uri.TryCreate(Api.BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"Modules:Api:Api:BaseUrl is not a valid absolute URI: '{Api.BaseUrl}'.");
        }

        if (Api.TimeoutSeconds <= 0 || Api.Polling.TimeoutSeconds <= 0 || Api.Polling.IntervalMilliseconds <= 0)
        {
            throw new InvalidOperationException("API timeouts and polling interval must be positive.");
        }

        if (Api.RateLimit.MaxRetries < 0 || Api.RateLimit.DefaultDelaySeconds <= 0 || Api.RateLimit.MaxDelaySeconds <= 0)
        {
            throw new InvalidOperationException("Rate-limit configuration is invalid.");
        }

        if (!string.Equals(Reporting.Engine, "extent", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This ATF is Extent-only. Reporting:Engine must be 'extent'.");
        }

        if (Reporting.AssertionMaxDisplayCharacters <= 0)
        {
            throw new InvalidOperationException("Reporting:AssertionMaxDisplayCharacters must be positive.");
        }
    }

    private AtfModuleSettings GetModule(string moduleId) =>
        Modules.TryGetValue(moduleId, out var module)
            ? module
            : throw new InvalidOperationException($"Modules:{moduleId} configuration is required.");
}

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
    public string Users { get; init; } = "/public/v2/users";
    public string Posts { get; init; } = "/public/v2/posts";
    public string UserPosts { get; init; } = "/public/v2/users/{userId}/posts";
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
}
