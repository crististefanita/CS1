using Atf.Core.Configuration;

namespace Atf.Core.Reporting;

public sealed class ExtentCollectionFixture : IDisposable
{
    private readonly IAtfReportLifecycle _reportLifecycle;
    private int _disposed;

    public ExtentCollectionFixture()
        : this(new AtfConfigurationLoader(), new ExtentReportLifecycle())
    {
    }

    internal ExtentCollectionFixture(
        IAtfConfigurationLoader configurationLoader,
        IAtfReportLifecycle reportLifecycle)
    {
        ArgumentNullException.ThrowIfNull(configurationLoader);
        _reportLifecycle = reportLifecycle ?? throw new ArgumentNullException(nameof(reportLifecycle));
        Configuration = configurationLoader.Load();
        _reportLifecycle.Initialize(Configuration.Reporting, Configuration.Api);
    }

    public AtfConfiguration Configuration { get; }

    public ExtentTestScope StartTest(
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories) =>
        _reportLifecycle.StartTest(testClassName, scenarioName, description, categories);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _reportLifecycle.FinalizeReport();
        GC.SuppressFinalize(this);
    }
}
