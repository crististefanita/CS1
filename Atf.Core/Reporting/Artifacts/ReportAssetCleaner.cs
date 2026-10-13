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
        => CleanAsync(reportPath).AsTask().GetAwaiter().GetResult();

    public static ValueTask CleanAsync(
        string reportPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        var reportDirectory = Path.GetDirectoryName(Path.GetFullPath(reportPath));
        if (string.IsNullOrWhiteSpace(reportDirectory))
        {
            return ValueTask.CompletedTask;
        }

        var assetDirectory = Path.Combine(reportDirectory, "extent");
        return DefaultCoordinator.CleanDirectoryAsync(assetDirectory, cancellationToken);
    }

    /// <summary>
    /// Best-effort deletion for a private Extent staging directory. Extent can
    /// briefly retain a handle to a generated font while another report is
    /// being flushed; the async path keeps that retry outside the reporter
    /// thread while cleanup remains non-verdict-affecting.
    /// </summary>
    public static void CleanDirectory(string directoryPath)
        => CleanDirectoryAsync(directoryPath).AsTask().GetAwaiter().GetResult();

    public static ValueTask CleanDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        return DefaultCoordinator.CleanDirectoryAsync(Path.GetFullPath(directoryPath), cancellationToken);
    }
}
