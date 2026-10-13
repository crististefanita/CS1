using Atf.Core.Configuration.Models;
using System.Globalization;

namespace Atf.Core.Configuration.Validation;

/// <summary>Validates the Extent reporting contract.</summary>
internal static class ReportingConfigurationValidator
{
    public static void Validate(ReportingSettings reporting)
    {
        if (!string.Equals(reporting.Engine, "extent", StringComparison.OrdinalIgnoreCase))
        {
            ConfigurationRuleViolation.Throw("ATF-REPORT-001", "Modules:Api:Config:Reporting:Engine", "This ATF is Extent-only. Engine must be 'extent'.");
        }

        if (reporting.AssertionMaxDisplayCharacters <= 0)
        {
            ConfigurationRuleViolation.Throw("ATF-REPORT-002", "Modules:Api:Config:Reporting:AssertionMaxDisplayCharacters", "AssertionMaxDisplayCharacters must be positive.");
        }

        if (string.IsNullOrWhiteSpace(reporting.ReportPath) ||
            !string.Equals(Path.GetExtension(reporting.ReportPath), ".html", StringComparison.OrdinalIgnoreCase))
        {
            ConfigurationRuleViolation.Throw("ATF-REPORT-003", "Modules:Api:Config:Reporting:ReportPath", "ReportPath must be a non-empty .html path.");
        }

        try
        {
            _ = DateTime.Now.ToString(reporting.TimestampFormat, CultureInfo.InvariantCulture);
        }
        catch (FormatException exception)
        {
            throw new AtfConfigurationException("ATF-REPORT-004", "Modules:Api:Config:Reporting:TimestampFormat", "TimestampFormat is not a valid date/time format.", exception);
        }
    }
}
