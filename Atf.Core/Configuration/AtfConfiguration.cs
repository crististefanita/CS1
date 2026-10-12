using Atf.Core.Configuration.Models;
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
