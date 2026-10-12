using System.Reflection;

namespace Atf.Core.Reporting;

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

    public void Compose(string reportPath, Assembly? testAssembly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        var html = File.ReadAllText(reportPath);
        html = _assetInliner.Inline(html);
        if (html.Contains("id=\"atf-custom-theme\"", StringComparison.Ordinal))
        {
            _assetCleaner.Clean(reportPath);
            return;
        }

        var testTotal = AtfTestCaseInventoryReader.Read(testAssembly).TotalTestCases;
        html = html.Replace(
            "</head>",
            AtfReportTheme.BuildCss() + Environment.NewLine + "</head>",
            StringComparison.OrdinalIgnoreCase);
        html = html.Replace(
            "</body>",
            ExtentReportClientBehaviour.Build(testTotal) + Environment.NewLine + "</body>",
            StringComparison.OrdinalIgnoreCase);

        File.WriteAllText(reportPath, html);
        _assetCleaner.Clean(reportPath);
    }
}
