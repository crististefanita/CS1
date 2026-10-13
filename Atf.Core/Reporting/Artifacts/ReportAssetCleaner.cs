namespace Atf.Core.Reporting.Artifacts;

/// <summary>
/// Extent creates a sibling asset folder even in offline mode. The final ATF
/// artifact is self-contained, therefore this cleanup is explicit and isolated
/// from the renderer so its safety can be unit-tested independently.
/// </summary>
public sealed class ReportAssetCleaner
{
    private static readonly ReportAssetCleanupCoordinator DefaultCoordinator =
        ReportAssetCleanupCoordinator.CreateDefault();

    public static void Clean(string reportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        var reportDirectory = Path.GetDirectoryName(Path.GetFullPath(reportPath));
        if (string.IsNullOrWhiteSpace(reportDirectory))
        {
            return;
        }

        var assetDirectory = Path.Combine(reportDirectory, "extent");
        DefaultCoordinator.CleanDirectory(assetDirectory);
    }

    /// <summary>
    /// Best-effort deletion for a private Extent staging directory. Extent can
    /// briefly retain a handle to a generated font while another report is
    /// being flushed; cleanup must not turn an already-composed report into a
    /// failed test run.
    /// </summary>
    public static void CleanDirectory(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        DefaultCoordinator.CleanDirectory(Path.GetFullPath(directoryPath));
    }
}
