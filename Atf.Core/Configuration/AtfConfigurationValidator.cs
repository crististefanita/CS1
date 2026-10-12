using Atf.Core.Configuration.Models;
using System.Globalization;

namespace Atf.Core.Configuration;

/// <summary>
/// Validates one configuration boundary at a time while preserving the public
/// <see cref="AtfConfiguration.Validate"/> entry point. The validator is
/// intentionally stateless so loading and validation remain easy to test.
/// </summary>
internal static class AtfConfigurationValidator
{
    public static void Validate(AtfConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!configuration.Modules.ContainsKey("api"))
        {
            throw new InvalidOperationException("Modules:Api configuration is required.");
        }

        ValidateApi(configuration.Api);
        ValidateReporting(configuration.Reporting);
        ValidateRedaction(configuration.Api.Redaction);
        ValidateRoutes(configuration.Api.Routes);
        ValidateAuthProfiles(configuration.Api.AuthProfiles);
    }

    private static void ValidateApi(ApiSettings api)
    {
        if (string.IsNullOrWhiteSpace(api.BaseUrl))
        {
            throw new InvalidOperationException("Modules:Api:Api:BaseUrl is required.");
        }

        if (!Uri.TryCreate(api.BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"Modules:Api:Api:BaseUrl is not a valid absolute URI: '{api.BaseUrl}'.");
        }

        if (api.TimeoutSeconds <= 0 || api.Polling.TimeoutSeconds <= 0 || api.Polling.IntervalMilliseconds <= 0)
        {
            throw new InvalidOperationException("API timeouts and polling interval must be positive.");
        }

        if (api.RateLimit.MaxRetries < 0 || api.RateLimit.DefaultDelaySeconds <= 0 || api.RateLimit.MaxDelaySeconds <= 0 ||
            api.RateLimit.MaxDelaySeconds < api.RateLimit.DefaultDelaySeconds)
        {
            throw new InvalidOperationException("Rate-limit configuration is invalid: MaxDelaySeconds must be greater than or equal to DefaultDelaySeconds.");
        }

        if (string.IsNullOrWhiteSpace(api.DefaultAuthProfile) || !api.AuthProfiles.ContainsKey(api.DefaultAuthProfile))
        {
            throw new InvalidOperationException(
                $"Modules:Api:Api:DefaultAuthProfile '{api.DefaultAuthProfile}' must reference a configured auth profile.");
        }
    }

    private static void ValidateReporting(ReportingSettings reporting)
    {
        if (!string.Equals(reporting.Engine, "extent", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This ATF is Extent-only. Reporting:Engine must be 'extent'.");
        }

        if (reporting.AssertionMaxDisplayCharacters <= 0)
        {
            throw new InvalidOperationException("Reporting:AssertionMaxDisplayCharacters must be positive.");
        }

        if (string.IsNullOrWhiteSpace(reporting.ReportPath) ||
            !string.Equals(Path.GetExtension(reporting.ReportPath), ".html", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Reporting:ReportPath must be a non-empty .html path.");
        }

        try
        {
            _ = DateTime.Now.ToString(reporting.TimestampFormat, CultureInfo.InvariantCulture);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Reporting:TimestampFormat is not a valid date/time format.", exception);
        }
    }

    private static void ValidateRedaction(RedactionSettings redaction)
    {
        if (string.IsNullOrWhiteSpace(redaction.Replacement))
        {
            throw new InvalidOperationException("Modules:Api:Api:Redaction:Replacement is required.");
        }

        if (redaction.RedactLocalPaths && string.IsNullOrWhiteSpace(redaction.LocalPathReplacement))
        {
            throw new InvalidOperationException(
                "Modules:Api:Api:Redaction:LocalPathReplacement is required when RedactLocalPaths is enabled.");
        }
    }

    private static void ValidateRoutes(RouteSettings routes)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(RouteSettings.Users)] = routes.Users,
            [nameof(RouteSettings.Posts)] = routes.Posts,
            [nameof(RouteSettings.UserPosts)] = routes.UserPosts
        };

        foreach (var (name, path) in values)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
            {
                throw new InvalidOperationException($"Modules:Api:Api:Routes:{name} must be a non-empty relative path starting with '/'.");
            }
        }

        var duplicate = values.GroupBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Modules:Api:Api:Routes contains duplicate route paths: {string.Join(", ", duplicate.Select(pair => pair.Key))}.");
        }
    }

    private static void ValidateAuthProfiles(IReadOnlyDictionary<string, AuthProfileSettings> profiles)
    {
        var supportedAuthTypes = new[] { "none", "bearer", "apikey", "api-key", "basic", "custom" };
        foreach (var (name, profile) in profiles)
        {
            if (!supportedAuthTypes.Contains(profile.Type, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unsupported auth profile type '{profile.Type}' for '{name}'. Supported types: {string.Join(", ", supportedAuthTypes)}.");
            }

            if (string.Equals(profile.Type, "basic", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(profile.UsernameEnvironmentVariable) || string.IsNullOrWhiteSpace(profile.PasswordEnvironmentVariable)))
            {
                throw new InvalidOperationException(
                    $"Auth profile '{name}' of type basic must configure UsernameEnvironmentVariable and PasswordEnvironmentVariable.");
            }

            if ((string.Equals(profile.Type, "apikey", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(profile.Type, "api-key", StringComparison.OrdinalIgnoreCase)) &&
                string.IsNullOrWhiteSpace(profile.HeaderName))
            {
                throw new InvalidOperationException($"Auth profile '{name}' of type apikey must configure HeaderName.");
            }

            if (string.Equals(profile.Type, "custom", StringComparison.OrdinalIgnoreCase) &&
                (profile.Headers.Count == 0 || profile.Headers.Any(header =>
                    string.IsNullOrWhiteSpace(header.Key) || string.IsNullOrWhiteSpace(header.Value))))
            {
                throw new InvalidOperationException($"Auth profile '{name}' of type custom must configure non-empty headers.");
            }
        }
    }
}
