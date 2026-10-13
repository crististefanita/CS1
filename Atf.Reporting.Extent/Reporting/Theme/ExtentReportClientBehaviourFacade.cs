using Atf.Reporting.Extent.Theme.Client.Ports;
using Atf.Reporting.Extent.Theme.Client.Composition;

namespace Atf.Reporting.Extent.Theme.Client;

/// <summary>
/// Stable compatibility entry point used by the Extent document composer and
/// existing consumers. The implementation is replaceable without changing
/// the generated report contract.
/// </summary>
internal static class ExtentReportClientBehaviour
{
    public static string Build(ReportingSettings? settings = null) =>
        new ExtentReportClientProjectionComposer().Compose(settings);
}
