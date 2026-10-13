using Atf.Core.Reporting.Artifacts;

namespace Atf.Reporting.Extent.Lifecycle;

internal interface IExtentArtifactPublisher
{
    void Publish(ExtentReportPaths paths, ReportingSettings settings);
}

/// <summary>
/// Owns the final filesystem transition from Extent's private native output
/// to the single self-contained ATF artifact.
/// </summary>
internal sealed class ExtentArtifactPublisher : IExtentArtifactPublisher
{
    public void Publish(ExtentReportPaths paths, ReportingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            if (File.Exists(paths.NativePath))
            {
                // The overview is a run report, not an assembly-discovery
                // report. Totals are derived from emitted scenario cards.
                ExtentReportDocumentComposer.Compose(paths.NativePath, settings);
                File.Move(paths.NativePath, paths.PublishedPath);
            }
        }
        finally
        {
            if (Directory.Exists(paths.NativeDirectory))
            {
                ReportAssetCleaner.CleanDirectory(paths.NativeDirectory);
            }
        }
    }
}
