using Atf.Core.Configuration.Models;

namespace Atf.Core.Configuration.Validation;

/// <summary>Validates authentication profile types and type-specific fields.</summary>
internal static class AuthProfileConfigurationValidator
{
    private static readonly string[] SupportedAuthTypes = ["none", "bearer", "apikey", "api-key", "basic", "custom"];

    public static void Validate(IReadOnlyDictionary<string, AuthProfileSettings> profiles)
    {
        foreach (var (name, profile) in profiles)
        {
            if (!SupportedAuthTypes.Contains(profile.Type, StringComparer.OrdinalIgnoreCase))
            {
                ConfigurationRuleViolation.Throw("ATF-AUTH-002", $"Modules:Api:Api:AuthProfiles:{name}:Type", $"Unsupported auth profile type '{profile.Type}'. Supported types: {string.Join(", ", SupportedAuthTypes)}.");
            }

            if (string.Equals(profile.Type, "basic", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(profile.UsernameEnvironmentVariable) || string.IsNullOrWhiteSpace(profile.PasswordEnvironmentVariable)))
            {
                ConfigurationRuleViolation.Throw(
                    "ATF-AUTH-003", $"Modules:Api:Api:AuthProfiles:{name}",
                    $"Auth profile '{name}' of type basic must configure UsernameEnvironmentVariable and PasswordEnvironmentVariable.");
            }

            if ((string.Equals(profile.Type, "apikey", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(profile.Type, "api-key", StringComparison.OrdinalIgnoreCase)) &&
                string.IsNullOrWhiteSpace(profile.HeaderName))
            {
                ConfigurationRuleViolation.Throw("ATF-AUTH-004", $"Modules:Api:Api:AuthProfiles:{name}:HeaderName", $"Auth profile '{name}' of type apikey must configure HeaderName.");
            }

            if (string.Equals(profile.Type, "custom", StringComparison.OrdinalIgnoreCase) &&
                (profile.Headers.Count == 0 || profile.Headers.Any(header =>
                    string.IsNullOrWhiteSpace(header.Key) || string.IsNullOrWhiteSpace(header.Value))))
            {
                ConfigurationRuleViolation.Throw("ATF-AUTH-005", $"Modules:Api:Api:AuthProfiles:{name}:Headers", $"Auth profile '{name}' of type custom must configure non-empty headers.");
            }
        }
    }
}
