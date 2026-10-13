using Atf.Core.Configuration;
using System.Reflection;

namespace Atf.Reporting.Extent;

public sealed class ExtentCollectionFixture : IAtfApiReportSession, IDisposable
{
    private readonly IAtfReportSession _reportSession;
    private int _disposed;

    public ExtentCollectionFixture()
        : this(new AtfConfigurationLoader(), new ExtentReportLifecycle())
    {
    }

    internal ExtentCollectionFixture(
        IAtfConfigurationLoader configurationLoader,
        IAtfReportSession reportSession)
    {
        ArgumentNullException.ThrowIfNull(configurationLoader);
        _reportSession = reportSession ?? throw new ArgumentNullException(nameof(reportSession));
        Configuration = configurationLoader.Load();
        _reportSession.Initialize(Configuration.Reporting, Configuration.Api);
    }

    public AtfConfiguration Configuration { get; }

    public string ReportPath => _reportSession.ReportPath;

    public IAtfScenarioScope StartTest(
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories) =>
        _reportSession.StartTest(testClassName, scenarioName, description, categories);

    public void RegisterTestAssembly(Assembly testAssembly) =>
        _reportSession.RegisterTestAssembly(testAssembly);

    public void Initialize(ReportingSettings settings, ApiSettings? api = null) =>
        _reportSession.Initialize(settings, api);

    public void Flush() => _reportSession.Flush();

    public void FinalizeReport() => _reportSession.FinalizeReport();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _reportSession.FinalizeReport();
        GC.SuppressFinalize(this);
    }
}
