using Atf.Core.Configuration;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using System.Reflection;

namespace Atf.Core.Reporting;

/// <summary>
/// Process-scoped Extent lifecycle. The static boundary is deliberately small;
/// tree semantics live in <see cref="ExtentTestHierarchy"/> and presentation
/// composition lives in dedicated components.
/// </summary>
public static class ExtentManager
{
    private static readonly object Sync = new();
    private static ExtentReports? _reports;
    private static ExtentSparkReporter? _reporter;
    private static ExtentTestHierarchy? _hierarchy;
    private static ReportingSettings? _settings;
    private static string? _reportPath;
    private static Assembly? _testAssembly;
    private static bool _finalized;

    public static string ReportPath => _reportPath ?? throw new InvalidOperationException("Extent reporting has not been initialized.");

    public static void RegisterTestAssembly(Assembly testAssembly)
    {
        ArgumentNullException.ThrowIfNull(testAssembly);
        lock (Sync)
        {
            _testAssembly = testAssembly;
        }
    }

    public static void Initialize(ReportingSettings settings, ApiSettings? api = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (Sync)
        {
            if (_reports is not null && !_finalized)
            {
                return;
            }

            var path = ResolvePath(settings);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var reporter = new ExtentSparkReporter(path);
            reporter.Config.DocumentTitle = settings.DocumentTitle;
            reporter.Config.ReportName = settings.ReportName;
            reporter.Config.OfflineMode = true;
            if (!string.IsNullOrWhiteSpace(settings.TimestampFormat))
            {
                reporter.Config.TimeStampFormat = settings.TimestampFormat;
            }

            var reports = new ExtentReports();
            reports.AttachReporter(reporter);
            new ExtentReportEnvironment().Apply(reports, api);

            _reporter = reporter;
            _reports = reports;
            _hierarchy = new ExtentTestHierarchy();
            _settings = settings;
            _reportPath = path;
            _finalized = false;
        }
    }

    public static ExtentTestScope StartTest(
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories)
    {
        lock (Sync)
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

    public static void RecordScenarioStatus(string testClassName, Status status)
    {
        lock (Sync)
        {
            _hierarchy?.RecordScenarioStatus(testClassName, status);
        }
    }

    public static void Flush()
    {
        lock (Sync)
        {
            _reports?.Flush();
        }
    }

    public static void FinalizeReport()
    {
        lock (Sync)
        {
            if (_finalized)
            {
                return;
            }

            _reports?.Flush();
            if (_reportPath is not null)
            {
                new ExtentReportDocumentComposer().Compose(_reportPath, _testAssembly, _settings!);
            }

            _finalized = true;
        }
    }

    private static string ResolvePath(ReportingSettings settings)
    {
        var resolved = new ReportPathResolver().Resolve(settings.ReportPath);
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
