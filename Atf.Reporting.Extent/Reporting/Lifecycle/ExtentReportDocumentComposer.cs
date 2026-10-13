namespace Atf.Reporting.Extent.Lifecycle;

using Atf.Reporting.Extent.Lifecycle.Sanitization;

/// <summary>
/// Composes the final single-file report after Extent has flushed its native
/// document. Sanitization, overview pruning, and ATF resource injection are
/// delegated to focused collaborators.
/// </summary>
internal sealed class ExtentReportDocumentComposer
{
    public static void Compose(string reportPath, ReportingSettings settings)
        => ComposeAsync(reportPath, settings).AsTask().GetAwaiter().GetResult();

    public static async ValueTask ComposeAsync(
        string reportPath,
        ReportingSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
        ArgumentNullException.ThrowIfNull(settings);

        var html = File.ReadAllText(reportPath);
        html = ExtentNativeHtmlSanitizer.RemoveAtfOwnedBlocks(html);
        html = ExtentNativeOverviewPruner.RemoveNativeLogEventsCard(html);
        html = ExtentAssetInliner.Inline(html);
        html = ExtentNativeOverviewPruner.RemoveNativeEventsChart(html);
        html = ExtentNativeHtmlSanitizer.EnsureDarkExtentTheme(html);
        html = ExtentNativeTagSelectorSanitizer.KeepSelectionTags(html, settings.TagTaxonomy.CreatePolicy());

        html = html.Replace(
            "</head>",
            AtfReportTheme.BuildCss() + Environment.NewLine + "</head>",
            StringComparison.OrdinalIgnoreCase);
        html = html.Replace(
            "</body>",
            ExtentReportClientBehaviour.Build(settings) + Environment.NewLine + "</body>",
            StringComparison.OrdinalIgnoreCase);

        File.WriteAllText(reportPath, html);
        await ReportAssetCleaner.CleanAsync(reportPath, cancellationToken).ConfigureAwait(false);
    }
}
