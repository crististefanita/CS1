namespace Atf.Reporting.Extent.Scope.Ports;

/// <summary>
/// Owns only renderer-tree transitions for one scenario. Evidence and status
/// policy are separate ports so a lifecycle change cannot silently change the
/// diagnostic payload or verdict precedence.
/// </summary>
internal interface IPhaseTreeWriter
{
    ScenarioStepFrame? Current { get; }

    void StartStep(string title);
    void CompletePhase(string phase, AtfReportStatus status);
    void CompleteStep(AtfReportStatus status);
    void CompleteAll(AtfReportStatus status);
    void Drain();
}

/// <summary>
/// Publishes scenario evidence into the active renderer node. Implementations
/// are responsible for redaction and aggregate assertion-table ownership.
/// </summary>
internal interface IReportEvidencePublisher
{
    void Detail(string key, string value);
    void Json(string label, string json);
    void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions);
    void Log(AtfReportStatus status, string message);
    void Fail(string message, Exception? exception = null);
    void Skip(string reason);
    void PassTest(string message);
    void FailTest(Exception exception);
    void SkipTest(string reason);
    void FlushPendingDetails();
}

/// <summary>
/// Applies ATF status precedence without depending on the Extent status enum.
/// </summary>
internal interface IReportStatusPolicy
{
    bool HasWarnings { get; }

    void RecordWarning(string? phaseTitle);
    AtfReportStatus ResolveScenarioStatus(AtfReportStatus requestedStatus);
    AtfReportStatus ResolvePhaseStatus(string phase, AtfReportStatus requestedStatus);
    void CompletePhase(string phase);
}
