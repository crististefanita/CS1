using Atf.Core.Reporting.Artifacts;
using System.Globalization;

namespace Atf.Reporting.Extent.Lifecycle;

/// <summary>
/// Allocates the published report path and the private native Extent output.
/// Keeping path identity and temporary-render ownership outside the lifecycle
/// makes concurrent sessions deterministic and keeps the adapter focused on
/// report orchestration.
/// </summary>
internal interface IExtentReportPathAllocator
{
    ExtentReportPaths Allocate(ReportingSettings settings);
}

internal sealed class ExtentReportPathAllocator : IExtentReportPathAllocator
{
    private static readonly object Sync = new();
    private static readonly HashSet<string> ReservedPaths = new(StringComparer.OrdinalIgnoreCase);

    private readonly ReportPathResolver _resolver;
    private readonly Func<DateTime> _clock;
    private readonly Func<Guid> _runIdFactory;

    public ExtentReportPathAllocator(
        ReportPathResolver? resolver = null,
        Func<DateTime>? clock = null,
        Func<Guid>? runIdFactory = null)
    {
        _resolver = resolver ?? new ReportPathResolver();
        _clock = clock ?? (() => DateTime.Now);
        _runIdFactory = runIdFactory ?? Guid.NewGuid;
    }

    public ExtentReportPaths Allocate(ReportingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var resolved = _resolver.Resolve(settings.ReportPath);
        var reportDirectory = Path.GetDirectoryName(resolved) ?? Directory.GetCurrentDirectory();
        var extension = Path.GetExtension(resolved);
        var name = Path.GetFileNameWithoutExtension(resolved);
        var timestamp = settings.TimestampEnabled
            ? $"_{_clock().ToString(settings.TimestampFormat, CultureInfo.InvariantCulture)}"
            : string.Empty;
        var publishedPath = ReservePublishedPath(reportDirectory, name, timestamp, extension);
        var nativeDirectory = Path.Combine(reportDirectory, $".atf-render-{Guid.NewGuid():N}");
        var nativePath = Path.Combine(nativeDirectory, Path.GetFileName(publishedPath));

        Directory.CreateDirectory(nativeDirectory);
        return new ExtentReportPaths(publishedPath, nativePath, nativeDirectory);
    }

    private string ReservePublishedPath(
        string reportDirectory,
        string name,
        string timestamp,
        string extension)
    {
        var candidate = Path.Combine(reportDirectory, $"{name}{timestamp}{extension}");

        lock (Sync)
        {
            if (!File.Exists(candidate) && ReservedPaths.Add(candidate))
            {
                return candidate;
            }

            // A timestamp describes a run; it is not a unique identity. The
            // reservation happens before Extent starts writing the file.
            do
            {
                var runId = _runIdFactory().ToString("N")[..10];
                candidate = Path.Combine(reportDirectory, $"{name}{timestamp}_run-{runId}{extension}");
            }
            while (File.Exists(candidate) || !ReservedPaths.Add(candidate));

            return candidate;
        }
    }
}

internal sealed record ExtentReportPaths(
    string PublishedPath,
    string NativePath,
    string NativeDirectory);
