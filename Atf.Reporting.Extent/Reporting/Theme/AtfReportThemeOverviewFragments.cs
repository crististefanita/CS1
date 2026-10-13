namespace Atf.Reporting.Extent.Theme;

internal static class AtfReportThemeOverviewFragments
{
    public static string Build() =>
        AtfReportThemeOverviewBaseFragments.Build()
        + AtfReportThemeOverviewAuditFragments.Build();
}

