using Atf.Core.Configuration;
using AventStack.ExtentReports;

namespace Atf.Core.Reporting;

/// <summary>
/// Application-facing report lifecycle. The current implementation remains
/// Extent-backed, while tests and a future reporter can depend on this contract
/// instead of the static ExtentManager facade.
/// </summary>
public interface IAtfReportLifecycle
{
    string ReportPath { get; }

    void Initialize(ReportingSettings settings, ApiSettings? api = null);

    ExtentTestScope StartTest(
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories);

    void Flush();
    void FinalizeReport();
}

public sealed class ExtentReportLifecycle : IAtfReportLifecycle
{
    public string ReportPath => ExtentManager.ReportPath;

    public void Initialize(ReportingSettings settings, ApiSettings? api = null) =>
        ExtentManager.Initialize(settings, api);

    public ExtentTestScope StartTest(
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories) =>
        ExtentManager.StartTest(testClassName, scenarioName, description, categories);

    public void Flush() => ExtentManager.Flush();

    public void FinalizeReport() => ExtentManager.FinalizeReport();
}
