namespace Atf.Core.Reporting;

/// <summary>
/// Runner-neutral scope for one scenario. Concrete renderers implement the
/// evidence mechanics, while API orchestration depends only on this port.
/// </summary>
public interface IAtfScenarioScope : IAtfReporter, IDisposable
{
    void CompletePhase(string phase, AtfReportStatus status);

    void CompleteScenario(AtfReportStatus status);

    void PassTest(string message);

    void FailTest(Exception exception);

    void SkipTest(string reason);
}
