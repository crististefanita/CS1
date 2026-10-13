using System.Net;

namespace Atf.Reporting.Extent.Scope;

/// <summary>
/// Coordinates the renderer tree for one scenario. Evidence publication stays
/// in <see cref="ExtentScopeEvidenceWriter"/> and status precedence stays in
/// <see cref="ExtentScopeStatusPolicy"/>; this type only owns phase/request/
/// validation nesting and safe frame closure.
/// </summary>
internal sealed class ExtentScopeTreeCoordinator : IPhaseTreeWriter
{
    private readonly IExtentNodeAdapter _root;
    private readonly ExtentScopeFrameStack _frames;
    private readonly IReportStatusPolicy _statusPolicy;
    private readonly Action flushPendingDetails;
    private readonly Action popStep;

    public ExtentScopeTreeCoordinator(
        IExtentNodeAdapter root,
        ExtentScopeFrameStack frames,
        IReportStatusPolicy statusPolicy,
        Action flushPendingDetails,
        Action popStep)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _frames = frames ?? throw new ArgumentNullException(nameof(frames));
        _statusPolicy = statusPolicy ?? throw new ArgumentNullException(nameof(statusPolicy));
        this.flushPendingDetails = flushPendingDetails ?? throw new ArgumentNullException(nameof(flushPendingDetails));
        this.popStep = popStep ?? throw new ArgumentNullException(nameof(popStep));
    }

    public ScenarioStepFrame? Current => _frames.Current;

    public void StartStep(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (ExtentStepClassifier.IsPhase(title))
        {
            DrainCurrentSteps();
        }
        else if (ExtentStepClassifier.IsHttp(title))
        {
            CloseActiveNonPhaseStep();
        }
        else if (ExtentStepClassifier.IsValidation(title))
        {
            if (!TryGetCurrentPhase(out _) || Current is null || !ExtentStepClassifier.IsHttp(Current.Title))
            {
                throw new InvalidOperationException(
                    $"Validation '{title}' must start after an active HTTP request inside a lifecycle phase.");
            }

            // Request and validation are siblings under the phase. The
            // response remains represented by their ordered pair.
            flushPendingDetails();
            popStep();
        }

        var node = CurrentOrTest().CreateNode(WebUtility.HtmlEncode(title), string.Empty);
        _frames.Push(new ScenarioStepFrame(node, title));
    }

    public void CompletePhase(string phase, AtfReportStatus requestedStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);
        var effectiveStatus = _statusPolicy.ResolvePhaseStatus(phase, requestedStatus);

        if (requestedStatus == AtfReportStatus.Pass && _frames.Count > 0)
        {
            var phaseFrame = _frames.Find(frame =>
                string.Equals(frame.Title, phase, StringComparison.Ordinal));
            if (phaseFrame is not null)
            {
                ExtentPhaseContractValidator.EnsureSuccessfulPhaseHasContractValidation(phaseFrame);
            }
        }

        while (Current is not null && !string.Equals(Current.Title, phase, StringComparison.Ordinal))
        {
            flushPendingDetails();
            Current.Adapter.SetStatus(ExtentStatusMapper.ToExtent(
                requestedStatus == AtfReportStatus.Pass ? AtfReportStatus.Info : requestedStatus));
            popStep();
        }

        if (Current is not null)
        {
            flushPendingDetails();
            Current.Adapter.SetStatus(ExtentStatusMapper.ToExtent(effectiveStatus));
            popStep();
        }

        _statusPolicy.CompletePhase(phase);
    }

    public void CompleteStep(AtfReportStatus status)
    {
        if (Current is null)
        {
            return;
        }

        flushPendingDetails();
        Current.Adapter.SetStatus(ExtentStatusMapper.ToExtent(status));
        popStep();
    }

    public void CompleteAll(AtfReportStatus status)
    {
        DrainCurrentSteps(status);
    }

    public void Drain()
    {
        DrainCurrentSteps();
    }

    private void DrainCurrentSteps(AtfReportStatus? status = null)
    {
        while (Current is not null)
        {
            flushPendingDetails();
            if (status is not null)
            {
                Current.Adapter.SetStatus(ExtentStatusMapper.ToExtent(status.Value));
            }

            popStep();
        }
    }

    private void CloseActiveNonPhaseStep()
    {
        while (Current is not null && !ExtentStepClassifier.IsPhase(Current.Title))
        {
            flushPendingDetails();
            Current.Adapter.SetStatus(ExtentStatusMapper.ToExtent(AtfReportStatus.Info));
            popStep();
        }
    }

    private bool TryGetCurrentPhase(out ScenarioStepFrame phase)
    {
        var currentPhase = _frames.Find(frame => ExtentStepClassifier.IsPhase(frame.Title));
        if (currentPhase is not null)
        {
            phase = currentPhase;
            return true;
        }

        phase = null!;
        return false;
    }

    private IExtentNodeAdapter CurrentOrTest() => Current?.Adapter ?? _root;
}
