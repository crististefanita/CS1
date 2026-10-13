using Atf.Core.Security;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using System.Reflection;

namespace Atf.Reporting.Extent;

/// <summary>
/// Extent-backed implementation of the runner-neutral report session.
/// Extent-specific rendering is kept inside this adapter; callers only own
/// the session contract and the ATF status model.
/// </summary>
public sealed class ExtentReportLifecycle : IAtfReportSession
{
    private readonly object _sync = new();
    private readonly Lifecycle.ExtentReportPathAllocator _pathAllocator;
    private ExtentReports? _reports;
    private ExtentSparkReporter? _reporter;
    private ExtentTestHierarchy? _hierarchy;
    private ReportingSettings? _settings;
    private string? _reportPath;
    private string? _nativeReportPath;
    private string? _nativeReportDirectory;
    private Assembly? _testAssembly;
    private SensitiveDataRedactor _redactor = new();
    private bool _finalized;

    public ExtentReportLifecycle()
        : this(new Lifecycle.ExtentReportPathAllocator())
    {
    }

    internal ExtentReportLifecycle(Lifecycle.ExtentReportPathAllocator pathAllocator)
    {
        _pathAllocator = pathAllocator ?? throw new ArgumentNullException(nameof(pathAllocator));
    }

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
            var reportPaths = _pathAllocator.Allocate(settingsSnapshot);

            // Extent's native asset bootstrap writes an `extent` directory
            // beside its report. Keep that transient directory private to this
            // session; only the composed HTML is published to the final path.
            var reporter = new ExtentSparkReporter(reportPaths.NativePath);
            reporter.Config.DocumentTitle = settingsSnapshot.DocumentTitle;
            reporter.Config.ReportName = settingsSnapshot.ReportName;
            reporter.Config.OfflineMode = true;
            if (!string.IsNullOrWhiteSpace(settingsSnapshot.TimestampFormat))
            {
                reporter.Config.TimeStampFormat = settingsSnapshot.TimestampFormat;
            }

            var reports = new ExtentReports();
            reports.AttachReporter(reporter);
            Lifecycle.ExtentReportEnvironment.Apply(reports, apiSnapshot);

            _reporter = reporter;
            _reports = reports;
            _hierarchy = new ExtentTestHierarchy(_sync);
            _settings = settingsSnapshot;
            _redactor = new SensitiveDataRedactor(apiSnapshot?.Redaction);
            _reportPath = reportPaths.PublishedPath;
            _nativeReportPath = reportPaths.NativePath;
            _nativeReportDirectory = reportPaths.NativeDirectory;
            _finalized = false;
        }
    }

    public IAtfScenarioScope StartTest(
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
                _settings.AssertionMaxDisplayCharacters,
                _redactor);
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
            if (_reportPath is not null && _nativeReportPath is not null && _settings is not null)
            {
                // The overview is a run report, not an assembly-discovery report.
                // Its totals are derived in the browser from the scenario cards
                // emitted by this execution, including skipped outcomes.
                Lifecycle.ExtentReportDocumentComposer.Compose(_nativeReportPath, _settings);
                File.Move(_nativeReportPath, _reportPath);
                if (_nativeReportDirectory is not null && Directory.Exists(_nativeReportDirectory))
                {
                    ReportAssetCleaner.CleanDirectory(_nativeReportDirectory);
                }
            }

            _finalized = true;
        }
    }

}
