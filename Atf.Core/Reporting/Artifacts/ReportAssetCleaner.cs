namespace Atf.Core.Reporting.Artifacts;

/// <summary>
/// Extent creates a sibling asset folder even in offline mode. The final ATF
/// artifact is self-contained, therefore this cleanup is explicit and isolated
/// from the renderer so its safety can be unit-tested independently.
/// </summary>
public sealed class ReportAssetCleaner
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(40),
        TimeSpan.FromMilliseconds(80),
        TimeSpan.FromMilliseconds(160),
        TimeSpan.FromMilliseconds(320)
    ];

    public static void Clean(string reportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        var reportDirectory = Path.GetDirectoryName(Path.GetFullPath(reportPath));
        if (string.IsNullOrWhiteSpace(reportDirectory))
        {
            return;
        }

        var assetDirectory = Path.Combine(reportDirectory, "extent");
        TryDeleteDirectory(assetDirectory);
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
        TryDeleteDirectory(Path.GetFullPath(directoryPath));
    }

    private static void TryDeleteDirectory(string directoryPath)
    {
        for (var attempt = 0; attempt < RetryDelays.Length; attempt++)
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            try
            {
                Directory.Delete(directoryPath, recursive: true);
                return;
            }
            catch (IOException) when (attempt < RetryDelays.Length - 1)
            {
                Thread.Sleep(RetryDelays[attempt + 1]);
            }
            catch (UnauthorizedAccessException) when (attempt < RetryDelays.Length - 1)
            {
                Thread.Sleep(RetryDelays[attempt + 1]);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }
    }
}
