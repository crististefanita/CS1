namespace Atf.Reporting.Extent.Tests;

/// <summary>
/// Focused contract tests for the theme and native-document composition seam.
/// Keeping these assertions outside the broad component fixture makes a CSS or
/// sanitizer regression fail at the responsible boundary.
/// </summary>
public sealed class ReportThemeContractTests
{
    [Fact(DisplayName = "Report theme keeps palette ownership in central tokens")]
    public void ThemeUsesCentralPalette()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(AtfReportColors.Dark, css, StringComparison.Ordinal);
        Assert.Contains(AtfReportColors.JsonSurface, css, StringComparison.Ordinal);
        Assert.Contains($"--atf-text: {AtfReportColors.Text}", css, StringComparison.Ordinal);
        Assert.Contains("--atf-secondary-heading-text: var(--atf-accent)", css, StringComparison.Ordinal);
        Assert.Contains("tr.assertion-warning td", css, StringComparison.Ordinal);
        Assert.Contains("tr.assertion-skip td", css, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Report theme gives category status badges one stable height")]
    public void CategoryStatusBadgesUseOneHeight()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains("--atf-category-badge-height: 22px;", css, StringComparison.Ordinal);
        Assert.Contains(".category-view .badge.log {", css, StringComparison.Ordinal);
        Assert.Contains("height: var(--atf-category-badge-height) !important;", css, StringComparison.Ordinal);
        Assert.Contains("min-height: var(--atf-category-badge-height) !important;", css, StringComparison.Ordinal);
        Assert.Contains("align-items: center !important;", css, StringComparison.Ordinal);
        Assert.Contains("line-height: 1 !important;", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Native sanitizer preserves a dark Extent body contract")]
    public void NativeDocumentUsesDarkTheme()
    {
        Assert.Contains(
            "class=\"spa dark\"",
            ExtentNativeHtmlSanitizer.EnsureDarkExtentTheme("<body class=\"spa standard\"><main /></body>"),
            StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Native sanitizer removes only ATF-owned composition blocks")]
    public void NativeDocumentRemovesOwnedBlocks()
    {
        const string html = "<head><style id=\"atf-custom-theme\">old-css</style></head>" +
            "<body><script id='atf-custom-behaviour'>old-js</script><main /></body>";

        var cleaned = ExtentNativeHtmlSanitizer.RemoveAtfOwnedBlocks(html);

        Assert.DoesNotContain("old-css", cleaned, StringComparison.Ordinal);
        Assert.DoesNotContain("old-js", cleaned, StringComparison.Ordinal);
        Assert.Contains("<main />", cleaned, StringComparison.Ordinal);
    }
}
