using Atf.Core.Reporting.Ports;

namespace Atf.Core.Reporting;

/// <summary>
/// Runner-neutral scope for one scenario. Concrete renderers implement the
/// evidence mechanics, while API orchestration depends only on this port.
/// </summary>
public interface IAtfScenarioScope : IAtfReporter, IAtfScenarioCompletionReporter, IDisposable
{
    /// <summary>
    /// Indicates that diagnostic warning evidence was recorded for the scenario.
    /// Lifecycle orchestration uses this read-only signal to keep its semantic
    /// result aligned with the renderer without depending on Extent internals.
    /// </summary>
    bool HasWarnings { get; }

    void CompletePhase(string phase, AtfReportStatus status);

    void CompleteScenario(AtfReportStatus status);

}
