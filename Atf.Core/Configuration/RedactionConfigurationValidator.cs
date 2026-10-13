using Atf.Core.Configuration.Models;

namespace Atf.Core.Configuration.Validation;

/// <summary>Validates sensitive-data redaction settings.</summary>
internal static class RedactionConfigurationValidator
{
    public static void Validate(RedactionSettings redaction)
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
}
