using Atf.Core.Configuration.Models;

namespace Atf.Core.Configuration.Validation;

/// <summary>Validates API transport settings and cross-references.</summary>
internal static class ApiConfigurationValidator
{
    public static void Validate(ApiSettings api)
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
}
