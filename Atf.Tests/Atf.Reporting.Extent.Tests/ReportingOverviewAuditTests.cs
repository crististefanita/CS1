namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingOverviewAuditTests
{
    [Fact(DisplayName = "Tag audit removes redundant explanatory copy")]
    public void ClientBehaviourOmitsRedundantTagAuditDescription()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.DoesNotContain("Deduplicated API navigation metadata", script, StringComparison.Ordinal);
        Assert.DoesNotContain("atf-tag-audit-heading", script, StringComparison.Ordinal);
        Assert.Contains("ATF scenario identities by test class", script, StringComparison.Ordinal);
        Assert.Contains("Test packages", script, StringComparison.Ordinal);
        Assert.Contains("Other annotations / tags", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Tag audit headings share native Overview typography and colour")]
    public void ReportThemeUsesNativeOverviewHeadingContractForTagAuditCards()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(".dashboard-view .category-container > .card > .card-header p", css, StringComparison.Ordinal);
        Assert.Contains(".dashboard-view .sysenv-container > .card > .card-header p", css, StringComparison.Ordinal);
        Assert.Contains(".dashboard-view .atf-tag-card > .card-header p { margin: 0 !important; color: var(--atf-secondary-heading-text) !important; font-family: var(--atf-overview-font-family) !important; font-size: var(--atf-font-base-size) !important; font-weight: 400 !important;", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".atf-tag-audit-heading", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Tag audit cards use the native Tags and System Environment frame")]
    public void ReportThemeUsesNativeOverviewFrameForTagAuditCards()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(
            ".atf-tag-audit-disclosure {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border: 0 !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".atf-tag-card { min-width: 0 !important; border: 0 !important; border-radius: 0 !important; background: transparent !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .atf-tag-card { min-width: 0 !important; border: 0 !important; border-radius: 0 !important; background: transparent !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".atf-tag-card .card-header { min-height: 0 !important; padding: 14px 24px 16px !important; border: 1px solid var(--atf-border) !important; border-bottom: 0 !important; border-radius: 8px 8px 0 0 !important; background: var(--atf-secondary-heading-bg) !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .atf-tag-card > .card-header p { margin: 0 !important; color: var(--atf-secondary-heading-text) !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".atf-tag-scroll { height: var(--atf-audit-scroll-height) !important; max-height: var(--atf-audit-scroll-height) !important; overflow-x: hidden !important; overflow-y: auto !important; border: 1px solid var(--atf-border) !important; border-top: 0 !important; border-radius: 0 0 8px 8px !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".atf-tag-table { width: 100% !important; margin: 0 !important; table-layout: fixed !important; border: 0 !important;",
            css,
            StringComparison.Ordinal);
        Assert.DoesNotContain(".atf-tag-total", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Native Tags and System Environment tables reuse the audit panel cell contract")]
    public void NativeOverviewTablesUseSinglePanelAndAuditCellContract()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(
            ".dashboard-view .category-container > .card,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "min-width: 0 !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "background: transparent !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .category-container > .card > .card-header,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card > .card-header {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border: 1px solid var(--atf-border) !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border-bottom: 0 !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .category-container > .card > .card-body,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card > .card-body {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border-top: 0 !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border-radius: 0 0 8px 8px !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "overflow-y: auto !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .category-container > .card > .card-body > table,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card > .card-body > table {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "table-layout: fixed !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .category-container > .card > .card-body > table th,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card > .card-body > table td {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "padding: 7px 9px !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "font-size: var(--atf-overview-font-size) !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "line-height: 1.32 !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .atf-tag-card .atf-tag-table > thead > tr > th,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card > .card-body > table > tbody > tr > th {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border-color: var(--atf-border) !important;",
            css,
            StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Tag audit tables contain only a header and data rows")]
    public void ClientBehaviourDoesNotAddSyntheticAuditTotalRows()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.DoesNotContain("atf-tag-total", script, StringComparison.Ordinal);
        Assert.DoesNotContain("ATF scenario identities total", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Test packages total", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Other annotations / tags total", script, StringComparison.Ordinal);
        Assert.Contains("buildAuditCard('classes', 'ATF scenario identities by test class'", script, StringComparison.Ordinal);
        Assert.Contains("buildAuditCard('packages', 'Test packages'", script, StringComparison.Ordinal);
        Assert.Contains("buildAuditCard('other', 'Other annotations / tags'", script, StringComparison.Ordinal);
    }
}
