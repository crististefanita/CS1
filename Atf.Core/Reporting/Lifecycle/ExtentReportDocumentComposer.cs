namespace Atf.Core.Reporting.Lifecycle;

/// <summary>
/// Composes the final single-file report after Extent has flushed its native
/// document. This is the boundary between Extent's generated HTML and ATF's
/// portable visual/reporting contract.
/// </summary>
internal sealed class ExtentReportDocumentComposer
{
    private readonly ExtentAssetInliner _assetInliner;
    private readonly ReportAssetCleaner _assetCleaner;

    public ExtentReportDocumentComposer(
        ExtentAssetInliner? assetInliner = null,
        ReportAssetCleaner? assetCleaner = null)
    {
        _assetInliner = assetInliner ?? new ExtentAssetInliner();
        _assetCleaner = assetCleaner ?? new ReportAssetCleaner();
    }

    public void Compose(string reportPath, ReportingSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
        ArgumentNullException.ThrowIfNull(settings);

        var html = File.ReadAllText(reportPath);
        html = RemoveAtfOwnedBlocks(html);
        html = _assetInliner.Inline(html);
        html = EnsureDarkExtentTheme(html);

        html = html.Replace(
            "</head>",
            AtfReportTheme.BuildCss() + Environment.NewLine + "</head>",
            StringComparison.OrdinalIgnoreCase);
        html = html.Replace(
            "</body>",
            ExtentReportClientBehaviour.Build(settings) + Environment.NewLine + "</body>",
            StringComparison.OrdinalIgnoreCase);

        File.WriteAllText(reportPath, html);
        _assetCleaner.Clean(reportPath);
    }

    internal static string RemoveAtfOwnedBlocks(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"\s*<style\s+id=[""']atf-custom-theme[""'][^>]*>.*?</style>\s*",
            Environment.NewLine,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        return System.Text.RegularExpressions.Regex.Replace(
            html,
            @"\s*<script\s+id=[""']atf-custom-behaviour[""'][^>]*>.*?</script>\s*",
            Environment.NewLine,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
    }

    internal static string EnsureDarkExtentTheme(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        return System.Text.RegularExpressions.Regex.Replace(
            html,
            "class=([\"'])spa(\\s+)standard\\1",
            "class=$1spa$2dark$1",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
