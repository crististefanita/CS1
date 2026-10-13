namespace Atf.Reporting.Extent.Scope;

/// <summary>
/// Keeps the semantic status rules for one scenario separate from Extent tree
/// navigation. A warning is diagnostic evidence: it must not be promoted to a
/// failure, but a nominally passing scenario/phase must not hide it either.
/// </summary>
internal sealed class ExtentScopeStatusPolicy : IReportStatusPolicy
{
    private readonly HashSet<string> _warningPhases = new(StringComparer.Ordinal);

    public bool HasWarnings { get; private set; }

    public void RecordWarning(string? phaseTitle)
    {
        HasWarnings = true;

        if (!string.IsNullOrWhiteSpace(phaseTitle))
        {
            _warningPhases.Add(phaseTitle);
        }
    }

    public AtfReportStatus ResolveScenarioStatus(AtfReportStatus requestedStatus) =>
        requestedStatus == AtfReportStatus.Pass && HasWarnings
            ? AtfReportStatus.Warning
            : requestedStatus;

    public AtfReportStatus ResolvePhaseStatus(string phaseTitle, AtfReportStatus requestedStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phaseTitle);

        return requestedStatus == AtfReportStatus.Pass && _warningPhases.Contains(phaseTitle)
            ? AtfReportStatus.Warning
            : requestedStatus;
    }

    public void CompletePhase(string phaseTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phaseTitle);
        _warningPhases.Remove(phaseTitle);
    }
}
