using Atf.Core.Configuration;
using Atf.Core.Configuration.Models;
using Atf.Core.Reporting;

namespace Atf.Api.TestHost;

/// <summary>
/// Live API-test composition fixture. The acceptance assembly depends on this
/// neutral host contract; only this adapter knows that the current renderer is
/// Extent.
/// </summary>
public sealed class ApiTestFixture : IAtfApiReportSession, IDisposable
{
    private readonly Atf.Reporting.Extent.ExtentCollectionFixture _extentFixture;

    public ApiTestFixture()
    {
        _extentFixture = new Atf.Reporting.Extent.ExtentCollectionFixture();
    }

    public AtfConfiguration Configuration => _extentFixture.Configuration;

    public string ReportPath => _extentFixture.ReportPath;

    public void RegisterTestAssembly(System.Reflection.Assembly testAssembly) =>
        _extentFixture.RegisterTestAssembly(testAssembly);

    public void Initialize(ReportingSettings settings, ApiSettings? api = null) =>
        _extentFixture.Initialize(settings, api);

    public IAtfScenarioScope StartTest(
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories) =>
        _extentFixture.StartTest(testClassName, scenarioName, description, categories);

    public void Flush() => _extentFixture.Flush();

    public void FinalizeReport() => _extentFixture.FinalizeReport();

    public void Dispose() => _extentFixture.Dispose();
}
