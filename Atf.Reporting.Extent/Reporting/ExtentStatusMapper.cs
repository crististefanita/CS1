using AventStack.ExtentReports;

namespace Atf.Reporting.Extent;

internal static class ExtentStatusMapper
{
    public static Status ToExtent(AtfReportStatus status) => status switch
    {
        AtfReportStatus.Info => Status.Info,
        AtfReportStatus.Pass => Status.Pass,
        AtfReportStatus.Fail => Status.Fail,
        AtfReportStatus.Skip => Status.Skip,
        AtfReportStatus.Warning => Status.Warning,
        _ => Status.Info
    };
}
