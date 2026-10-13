using Atf.Core.Configuration.Models;

namespace Atf.Core.Configuration.Validation;

/// <summary>Validates sensitive-data redaction settings.</summary>
internal static class RedactionConfigurationValidator
{
    public static void Validate(RedactionSettings redaction)
    {
        if (string.IsNullOrWhiteSpace(redaction.Replacement))
        {
            ConfigurationRuleViolation.Throw("ATF-REDACT-001", "Modules:Api:Api:Redaction:Replacement", "Replacement is required.");
        }

        if (redaction.RedactLocalPaths && string.IsNullOrWhiteSpace(redaction.LocalPathReplacement))
        {
            ConfigurationRuleViolation.Throw(
                "ATF-REDACT-002", "Modules:Api:Api:Redaction:LocalPathReplacement",
                "LocalPathReplacement is required when RedactLocalPaths is enabled.");
        }
    }
}
