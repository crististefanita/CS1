namespace Atf.Core.Reporting.Artifacts;

/// <summary>
/// Extent creates a sibling asset folder even in offline mode. The final ATF
/// artifact is self-contained, therefore this cleanup is explicit and isolated
/// from the renderer so its safety can be unit-tested independently.
/// </summary>
public sealed class ReportAssetCleaner
{
    public void Clean(string reportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        var reportDirectory = Path.GetDirectoryName(Path.GetFullPath(reportPath));
        if (string.IsNullOrWhiteSpace(reportDirectory))
        {
            return;
        }

        var assetDirectory = Path.Combine(reportDirectory, "extent");
        if (Directory.Exists(assetDirectory))
        {
            Directory.Delete(assetDirectory, recursive: true);
        }
    }
}
