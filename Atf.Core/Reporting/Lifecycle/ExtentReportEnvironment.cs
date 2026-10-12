using Atf.Core.Configuration;
using AventStack.ExtentReports;
using System.Runtime.InteropServices;

namespace Atf.Core.Reporting.Lifecycle;

/// <summary>
/// Owns the environment contract exposed by an Extent report. Keeping the
/// ordered metadata definition here prevents the report lifecycle from knowing
/// how environment values are named or sourced.
/// </summary>
internal sealed class ExtentReportEnvironment
{
    public void Apply(ExtentReports reports, ApiSettings? api)
    {
        ArgumentNullException.ThrowIfNull(reports);

        foreach (var entry in Entries(api))
        {
            reports.AddSystemInfo(entry.Name, entry.Value);
        }
    }

    private static IEnumerable<EnvironmentEntry> Entries(ApiSettings? api)
    {
        yield return new("Environment", Environment.GetEnvironmentVariable("ATF_ENV")?.Trim() ?? "LOCAL");
        yield return new("API base URL", api?.BaseUrl ?? "not configured");
        yield return new("Runtime", RuntimeInformation.FrameworkDescription);
        yield return new("OS", RuntimeInformation.OSDescription);
        yield return new("Client", "HttpClient");
        yield return new("Authentication", api?.DefaultAuthProfile ?? "not configured");
        yield return new("Write tests", api?.WriteTestsEnabled == true ? "enabled" : "disabled");
        yield return new("Test package", Environment.GetEnvironmentVariable("ATF_TEST_PACKAGE")?.Trim() ?? "Atf.Api");
        yield return new("Reporting", "Extent · embedded offline HTML");
    }

    private sealed record EnvironmentEntry(string Name, string Value);
}
