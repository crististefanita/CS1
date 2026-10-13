using Atf.Core.Reporting.Ports;

namespace Atf.Core.Reporting;

/// <summary>
/// Runner-neutral scope for one scenario. Concrete renderers implement the
/// evidence mechanics, while API orchestration depends only on this port.
/// </summary>
public interface IAtfScenarioScope : IAtfReporter, IAtfScenarioCompletionReporter, IDisposable
{
    void CompletePhase(string phase, AtfReportStatus status);

    void CompleteScenario(AtfReportStatus status);

}
