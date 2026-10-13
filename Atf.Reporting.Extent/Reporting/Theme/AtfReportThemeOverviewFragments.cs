namespace Atf.Reporting.Extent.Theme.Css;

internal static class AtfReportThemeOverviewFragments
{
    public static string Build() =>
        AtfReportThemeOverviewBaseFragments.Build()
        + AtfReportThemeOverviewAuditFragments.Build();
}

