using Atf.Core.Configuration;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using System.Reflection;

namespace Atf.Core.Reporting;

/// <summary>
/// One isolated report session for one test run. The session owns the Extent
/// document and its hierarchy; it is intentionally instance-scoped so a
/// parallel run cannot reuse another run's class nodes, status aggregation or
/// output path.
/// </summary>
public interface IAtfReportSession
{
    string ReportPath { get; }

    void RegisterTestAssembly(Assembly testAssembly);

    void Initialize(ReportingSettings settings, ApiSettings? api = null);

    ExtentTestScope StartTest(
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories);

    void Flush();
    void FinalizeReport();
}

/// <summary>
/// Extent-backed implementation of the runner-neutral report session.
/// Extent-specific rendering is kept inside this adapter; callers only own
/// the session contract and the ATF status model.
/// </summary>
public sealed class ExtentReportLifecycle : IAtfReportSession
{
    private readonly object _sync = new();
    private ExtentReports? _reports;
    private ExtentSparkReporter? _reporter;
    private ExtentTestHierarchy? _hierarchy;
    private ReportingSettings? _settings;
    private string? _reportPath;
    private Assembly? _testAssembly;
    private bool _finalized;

    public string ReportPath => _reportPath ?? throw new InvalidOperationException("Extent reporting has not been initialized.");

    public void RegisterTestAssembly(Assembly testAssembly)
    {
        ArgumentNullException.ThrowIfNull(testAssembly);
        lock (_sync)
        {
            _testAssembly = testAssembly;
        }
    }

    public void Initialize(ReportingSettings settings, ApiSettings? api = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_sync)
        {
            if (_reports is not null && !_finalized)
            {
                return;
            }

            var settingsSnapshot = settings.Snapshot();
            var apiSnapshot = api?.Snapshot();
            var path = ResolvePath(settingsSnapshot);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var reporter = new ExtentSparkReporter(path);
            reporter.Config.DocumentTitle = settingsSnapshot.DocumentTitle;
            reporter.Config.ReportName = settingsSnapshot.ReportName;
            reporter.Config.OfflineMode = true;
            if (!string.IsNullOrWhiteSpace(settingsSnapshot.TimestampFormat))
            {
                reporter.Config.TimeStampFormat = settingsSnapshot.TimestampFormat;
            }

            var reports = new ExtentReports();
            reports.AttachReporter(reporter);
            new Lifecycle.ExtentReportEnvironment().Apply(reports, apiSnapshot);

            _reporter = reporter;
            _reports = reports;
            _hierarchy = new ExtentTestHierarchy();
            _settings = settingsSnapshot;
            _reportPath = path;
            _finalized = false;
        }
    }

    public ExtentTestScope StartTest(
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories)
    {
        lock (_sync)
        {
            if (_reports is null || _hierarchy is null || _settings is null)
            {
                throw new InvalidOperationException("Extent reporting must be initialized before starting a test.");
            }

            return _hierarchy.Start(
                _reports,
                testClassName,
                scenarioName,
                description,
                categories,
                _settings.AssertionMaxDisplayCharacters);
        }
    }

    public void Flush()
    {
        lock (_sync)
        {
            _reports?.Flush();
        }
    }

    public void FinalizeReport()
    {
        lock (_sync)
        {
            if (_finalized)
            {
                return;
            }

            _reports?.Flush();
            if (_reportPath is not null && _settings is not null)
            {
                new Lifecycle.ExtentReportDocumentComposer().Compose(_reportPath, _settings);
            }

            _finalized = true;
        }
    }

    private static string ResolvePath(ReportingSettings settings)
    {
        var resolved = new Artifacts.ReportPathResolver().Resolve(settings.ReportPath);
        if (!settings.TimestampEnabled)
        {
            return resolved;
        }

        var directory = Path.GetDirectoryName(resolved) ?? Directory.GetCurrentDirectory();
        var extension = Path.GetExtension(resolved);
        var name = Path.GetFileNameWithoutExtension(resolved);
        var timestamp = DateTime.Now.ToString(settings.TimestampFormat);
        return Path.Combine(directory, $"{name}_{timestamp}{extension}");
    }
}
