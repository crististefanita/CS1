using Atf.Core.Reporting.Artifacts;

namespace Atf.Reporting.Extent.Lifecycle;

internal interface IExtentArtifactPublisher
{
    void Publish(ExtentReportPaths paths, ReportingSettings settings);

    ValueTask PublishAsync(
        ExtentReportPaths paths,
        ReportingSettings settings,
        CancellationToken cancellationToken = default)
    {
        Publish(paths, settings);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Owns the final filesystem transition from Extent's private native output
/// to the single self-contained ATF artifact.
/// </summary>
internal sealed class ExtentArtifactPublisher : IExtentArtifactPublisher
{
    public void Publish(ExtentReportPaths paths, ReportingSettings settings)
        => PublishAsync(paths, settings).AsTask().GetAwaiter().GetResult();

    public async ValueTask PublishAsync(
        ExtentReportPaths paths,
        ReportingSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            if (File.Exists(paths.NativePath))
            {
                // The overview is a run report, not an assembly-discovery
                // report. Totals are derived from emitted scenario cards.
                await ExtentReportDocumentComposer.ComposeAsync(
                    paths.NativePath,
                    settings,
                    cancellationToken).ConfigureAwait(false);
                File.Move(paths.NativePath, paths.PublishedPath);
            }
        }
        finally
        {
            if (Directory.Exists(paths.NativeDirectory))
            {
                await ReportAssetCleaner.CleanDirectoryAsync(
                    paths.NativeDirectory,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
