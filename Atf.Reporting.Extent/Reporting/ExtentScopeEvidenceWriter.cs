using Atf.Core.Security;
using System.Net;

namespace Atf.Reporting.Extent;

/// <summary>
/// Owns the evidence boundary for one Extent scenario. It is deliberately
/// independent from the step-stack coordinator: redaction, HTML composition
/// and assertion aggregation can be tested without exercising lifecycle rules.
/// The caller serializes access through the scenario scope gate.
/// </summary>
internal sealed class ExtentScopeEvidenceWriter
{
    private readonly IExtentNodeAdapter _root;
    private readonly Func<ExtentTestScope.StepFrame?> _current;
    private readonly SensitiveDataRedactor _redactor;

    public ExtentScopeEvidenceWriter(
        IExtentNodeAdapter root,
        Func<ExtentTestScope.StepFrame?> current,
        int assertionMaxDisplayCharacters,
        SensitiveDataRedactor redactor)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _current = current ?? throw new ArgumentNullException(nameof(current));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assertionMaxDisplayCharacters);
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        AssertionMaxDisplayCharacters = assertionMaxDisplayCharacters;
    }

    public int AssertionMaxDisplayCharacters { get; }

    public void Detail(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var frame = _current();
        if (frame is null)
        {
            _root.Info(EvidenceTableFormatter.BuildTechnicalTraceTableHtml(
                [new KeyValuePair<string, string>(key, _redactor.RedactField(key, value ?? string.Empty))]));
            return;
        }

        frame.Details.Add((key, _redactor.RedactField(key, value ?? string.Empty)));
    }

    public void Json(string label, string json)
    {
        FlushPendingDetails();
        var safeJson = _redactor.RedactJson(json);
        var jsonLabel = ExtentEvidenceRenderer.JsonDisplayLabel(label);
        var body = $"<div class='helm-scroll-block'><div class='helm-scroll-block-title'>{ExtentEvidenceRenderer.Html(jsonLabel)}</div>" +
                   $"<div class='helm-scroll-block-body'><pre>{ExtentEvidenceRenderer.Html(safeJson)}</pre></div></div>";
        CurrentOrTest().Info(ExtentEvidenceRenderer.Build(jsonLabel, body, "json-evidence"));
    }

    public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);
        var frame = _current();
        if (frame is null || !ExtentStepClassifier.IsValidation(frame.Title))
        {
            throw new InvalidOperationException(
                "AssertionTable can only be published by an active 'Validate:' step paired with an API request.");
        }

        if (frame.AssertionTableCount != 0)
        {
            throw new InvalidOperationException(
                $"Validation '{frame.Title}' already owns an aggregate AssertionTable.");
        }

        FlushPendingDetails();
        var safeAssertions = assertions
            .Select(assertion => assertion with
            {
                Expected = _redactor.RedactText(assertion.Expected),
                Actual = _redactor.RedactText(assertion.Actual)
            })
            .ToArray();
        CurrentOrTest().Info(ExtentEvidenceRenderer.Build(
            $"Assertions · {safeAssertions.Length}",
            AssertionEvidenceFormatter.BuildTableHtml(safeAssertions, AssertionMaxDisplayCharacters),
            "assertion-evidence"));
        frame.AssertionTableCount = 1;
    }

    public void Log(AtfReportStatus status, string message) =>
        CurrentOrTest().Log(
            ExtentStatusMapper.ToExtent(status),
            ExtentEvidenceRenderer.Html(_redactor.RedactText(message ?? string.Empty)));

    public void Fail(string message, Exception? exception = null)
    {
        var detail = exception is null
            ? _redactor.RedactText(message)
            : $"{_redactor.RedactText(message)}<pre>{ExtentEvidenceRenderer.Html(_redactor.RedactText(exception.ToString()))}</pre>";
        CurrentOrTest().Fail(ExtentEvidenceRenderer.Build("Failure", detail, "fail"));
    }

    public void Skip(string reason) =>
        CurrentOrTest().Skip(ExtentEvidenceRenderer.Build(
            "Skipped",
            ExtentEvidenceRenderer.Html(_redactor.RedactText(reason)),
            "skip"));

    public void PassTest(string message) =>
        _root.Pass(ExtentEvidenceRenderer.Build(
            "Scenario passed",
            ExtentEvidenceRenderer.Html(_redactor.RedactText(message))));

    public void FailTest(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _root.Fail(ExtentEvidenceRenderer.Build(
            "Failure",
            $"<pre>{ExtentEvidenceRenderer.Html(_redactor.RedactText(exception.ToString()))}</pre>",
            "fail"));
    }

    public void SkipTest(string reason) =>
        _root.Skip(ExtentEvidenceRenderer.Html(_redactor.RedactText(reason)));

    public void FlushPendingDetails()
    {
        var frame = _current();
        if (frame is null || frame.Details.Count == 0)
        {
            return;
        }

        if (ExtentStepClassifier.IsPhase(frame.Title))
        {
            // Lifecycle phases are structural containers. Generic Extent
            // summaries add noise and do not own a response verdict.
            frame.Details.Clear();
            return;
        }

        var evidence = ExecutionEvidenceComposer.Compose(
            frame.Title,
            frame.Details.Select(detail => new KeyValuePair<string, string>(detail.Key, detail.Value)).ToArray());
        if (evidence.IsGenericSummary)
        {
            frame.Details.Clear();
            return;
        }

        var body = evidence.IsAssertion
            ? evidence.BodyHtml
            : "<div class='helm-api-classification technical'><span class='helm-api-classification-label'>TECHNICAL TRACE</span></div>" + evidence.BodyHtml;
        frame.Adapter.Info(ExtentEvidenceRenderer.Build(evidence.Title, body, evidence.Kind));
        frame.Details.Clear();
    }

    private IExtentNodeAdapter CurrentOrTest() => _current()?.Adapter ?? _root;
}
