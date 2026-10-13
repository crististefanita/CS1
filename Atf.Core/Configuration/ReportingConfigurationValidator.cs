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
}
