using System.Collections.Concurrent;

namespace Atf.Core.Reporting.Artifacts;

/// <summary>
/// Coordinates best-effort deletion of renderer-owned directories.
/// A report can be finalized by more than one worker, so the coordination
/// boundary is the normalized directory rather than the report instance.
/// </summary>
internal sealed class ReportAssetCleanupCoordinator
{
    private static readonly ConcurrentDictionary<string, DirectoryGate> DirectoryLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object DirectoryLocksSync = new();

    private readonly IReportAssetFileSystem _fileSystem;
    private readonly IReadOnlyList<TimeSpan> _retryDelays;

    internal ReportAssetCleanupCoordinator(
        IReportAssetFileSystem fileSystem,
        IReadOnlyList<TimeSpan> retryDelays)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _retryDelays = retryDelays is { Count: > 0 }
            ? retryDelays
            : throw new ArgumentException("At least one cleanup attempt is required.", nameof(retryDelays));
    }

    internal static ReportAssetCleanupCoordinator CreateDefault() => new(
        new PhysicalReportAssetFileSystem(),
        [
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(40),
            TimeSpan.FromMilliseconds(80),
            TimeSpan.FromMilliseconds(160),
            TimeSpan.FromMilliseconds(320)
        ]);

    internal void CleanDirectory(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        var normalizedPath = Path.GetFullPath(directoryPath);
        DirectoryGate gate;
        lock (DirectoryLocksSync)
        {
            gate = DirectoryLocks.GetOrAdd(normalizedPath, static _ => new DirectoryGate());
            gate.ReferenceCount++;
        }

        lock (gate.Sync)
        {
            try
            {
                TryDelete(normalizedPath);
            }
            finally
            {
                ReleaseGate(normalizedPath, gate);
            }
        }
    }

    internal static int ActiveDirectoryGateCount
    {
        get
        {
            lock (DirectoryLocksSync)
            {
                return DirectoryLocks.Count;
            }
        }
    }

    private static void ReleaseGate(string normalizedPath, DirectoryGate gate)
    {
        lock (DirectoryLocksSync)
        {
            gate.ReferenceCount--;
            if (gate.ReferenceCount == 0)
            {
                ((ICollection<KeyValuePair<string, DirectoryGate>>)DirectoryLocks)
                    .Remove(new KeyValuePair<string, DirectoryGate>(normalizedPath, gate));
            }
        }
    }

    private void TryDelete(string directoryPath)
    {
        for (var attempt = 0; attempt < _retryDelays.Count; attempt++)
        {
            if (!_fileSystem.DirectoryExists(directoryPath))
            {
                return;
            }

            try
            {
                _fileSystem.DeleteDirectory(directoryPath);
                return;
            }
            catch (IOException) when (attempt < _retryDelays.Count - 1)
            {
                _fileSystem.Delay(_retryDelays[attempt + 1]);
            }
            catch (UnauthorizedAccessException) when (attempt < _retryDelays.Count - 1)
            {
                _fileSystem.Delay(_retryDelays[attempt + 1]);
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

    private sealed class DirectoryGate
    {
        public object Sync { get; } = new();
        public int ReferenceCount { get; set; }
    }
}

internal interface IReportAssetFileSystem
{
    bool DirectoryExists(string directoryPath);
    void DeleteDirectory(string directoryPath);
    void Delay(TimeSpan delay);
}

internal sealed class PhysicalReportAssetFileSystem : IReportAssetFileSystem
{
    public bool DirectoryExists(string directoryPath) => Directory.Exists(directoryPath);

    public void DeleteDirectory(string directoryPath) => Directory.Delete(directoryPath, recursive: true);

    public void Delay(TimeSpan delay) => Thread.Sleep(delay);
}
