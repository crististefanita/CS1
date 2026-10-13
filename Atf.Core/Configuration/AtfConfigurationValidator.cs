using Atf.Core.Configuration.Models;
using Atf.Core.Configuration.Validation;

namespace Atf.Core.Configuration;

/// <summary>
/// Coordinates independent configuration-boundary validators while preserving
/// the public <see cref="AtfConfiguration.Validate"/> entry point.
/// </summary>
internal static class AtfConfigurationValidator
{
    public static void Validate(AtfConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!configuration.Modules.ContainsKey("api"))
        {
            ConfigurationRuleViolation.Throw("ATF-MODULE-001", "Modules:Api", "API module configuration is required.");
        }

        ApiConfigurationValidator.Validate(configuration.Api);
        ReportingConfigurationValidator.Validate(configuration.Reporting);
        RedactionConfigurationValidator.Validate(configuration.Api.Redaction);
        RouteConfigurationValidator.Validate(configuration.Api.Routes);
        AuthProfileConfigurationValidator.Validate(configuration.Api.AuthProfiles);
    }
}
