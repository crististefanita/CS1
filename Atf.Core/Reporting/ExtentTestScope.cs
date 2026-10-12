using AventStack.ExtentReports;
using System.Net;

namespace Atf.Core.Reporting;

/// <summary>
/// Scenario-scoped adapter over Extent. It owns the execution tree and keeps
/// report evidence attached to the currently active API step.
/// </summary>
public sealed class ExtentTestScope : IAtfReporter, IDisposable
{
    private readonly ExtentTest _test;
    private readonly string _testClassName;
    private readonly Action<string, AtfReportStatus> _recordScenarioStatus;
    private readonly AsyncLocal<Stack<StepFrame>?> _frames = new();
    private readonly ExtentEvidenceRenderer _evidence = new();
    private int _disposed;

    internal ExtentTestScope(
        ExtentTest test,
        string testClassName,
        int assertionMaxDisplayCharacters = ReportingDefaults.AssertionMaxDisplayCharacters)
        : this(test, testClassName, static (_, _) => { }, assertionMaxDisplayCharacters)
    {
    }

    internal ExtentTestScope(
        ExtentTest test,
        string testClassName,
        Action<string, AtfReportStatus> recordScenarioStatus,
        int assertionMaxDisplayCharacters = ReportingDefaults.AssertionMaxDisplayCharacters)
    {
        _test = test ?? throw new ArgumentNullException(nameof(test));
        ArgumentException.ThrowIfNullOrWhiteSpace(testClassName);
        ArgumentNullException.ThrowIfNull(recordScenarioStatus);
        if (assertionMaxDisplayCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assertionMaxDisplayCharacters));
        }

        _testClassName = testClassName;
        _recordScenarioStatus = recordScenarioStatus;
        AssertionMaxDisplayCharacters = assertionMaxDisplayCharacters;
    }

    public int AssertionMaxDisplayCharacters { get; }

    public StepFrame? Current => _frames.Value is { Count: > 0 } frames ? frames.Peek() : null;

    public void CompleteScenario(AtfReportStatus status)
    {
        FlushPendingDetails();
        while (Current is not null)
        {
            Current.Node.Model.Status = ExtentStatusMapper.ToExtent(status);
            PopStep();
        }

        _test.Model.Status = ExtentStatusMapper.ToExtent(status);
        _recordScenarioStatus(_testClassName, status);
    }

    public void StartStep(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        // Lifecycle phases are the direct children of a scenario. An API
        // request and each validation are separate executable steps beneath
        // the phase. This mirrors the Extent UI contract: TEST BODY contains
        // a request step followed by validation step siblings, while the
        // request itself owns only its response evidence.
        if (ApiScenarioPhaseLayout.Classify(title) is not null)
        {
            while (Current is not null)
            {
                FlushPendingDetails();
                PopStep();
            }
        }
        else if (IsValidationStepTitle(title) && TryGetCurrentPhase(out var phase))
        {
            // Validation is intentionally attached to the phase rather than
            // to the request currently on the stack. Keeping the request
            // frame alive preserves the defensive close-on-next-request
            // behaviour, while the Extent model remains phase -> request +
            // validation siblings.
            FlushPendingDetails();
            var validationNode = phase.Node.CreateNode(WebUtility.HtmlEncode(title), string.Empty);
            (_frames.Value ??= new Stack<StepFrame>()).Push(new StepFrame(validationNode, title));
            return;
        }
        else if (IsHttpStepTitle(title))
        {
            // A defensive boundary for callers that start a new exchange
            // without explicitly closing a previous non-phase step.
            while (Current is not null && ApiScenarioPhaseLayout.Classify(Current.Title) is null)
            {
                FlushPendingDetails();
                Current.Node.Model.Status = ExtentStatusMapper.ToExtent(AtfReportStatus.Info);
                PopStep();
            }
        }

        var node = CurrentOrTest().CreateNode(WebUtility.HtmlEncode(title), string.Empty);
        (_frames.Value ??= new Stack<StepFrame>()).Push(new StepFrame(node, title));
    }

    public void CompletePhase(string phase, AtfReportStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);

        while (Current is not null && !string.Equals(Current.Title, phase, StringComparison.Ordinal))
        {
            FlushPendingDetails();
            Current.Node.Model.Status = ExtentStatusMapper.ToExtent(status == AtfReportStatus.Pass ? AtfReportStatus.Info : status);
            PopStep();
        }

        if (Current is not null)
        {
            FlushPendingDetails();
            Current.Node.Model.Status = ExtentStatusMapper.ToExtent(status);
            PopStep();
        }
    }

    public void CompleteStep(AtfReportStatus status)
    {
        var frame = Current;
        if (frame is null)
        {
            return;
        }

        FlushPendingDetails();
        frame.Node.Model.Status = ExtentStatusMapper.ToExtent(status);
        PopStep();
    }

    public void Detail(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var frame = Current;
        if (frame is null)
        {
            _test.Info(EvidenceTableFormatter.BuildTechnicalTraceTableHtml(
                [new KeyValuePair<string, string>(key, value ?? string.Empty)]));
            return;
        }

        frame.Details.Add((key, value ?? string.Empty));
    }

    public void Json(string label, string json)
    {
        FlushPendingDetails();
        var body = $"<div class='helm-scroll-block'><div class='helm-scroll-block-title'>{_evidence.Html(_evidence.JsonDisplayLabel(label))}</div>" +
                   $"<div class='helm-scroll-block-body'><pre>{_evidence.Html(json ?? string.Empty)}</pre></div></div>";
        CurrentOrTest().Info(_evidence.Build(_evidence.JsonDisplayLabel(label), body, "json-evidence"));
    }

    public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);
        FlushPendingDetails();
        CurrentOrTest().Info(_evidence.Build(
            $"Assertions · {assertions.Count}",
            AssertionEvidenceFormatter.BuildTableHtml(assertions),
            "assertion-evidence"));
    }

    public void Info(string message) => Log(AtfReportStatus.Info, message);

    public void Pass(string message) => Log(AtfReportStatus.Pass, message);

    public void Fail(string message, Exception? exception = null)
    {
        var detail = exception is null
            ? message
            : $"{message}<pre>{_evidence.Html(exception.ToString())}</pre>";
        CurrentOrTest().Fail(_evidence.Build("Failure", detail, "fail"));
    }

    public void Warning(string message) => Log(AtfReportStatus.Warning, message);

    public void Skip(string reason) => CurrentOrTest().Skip(_evidence.Build("Skipped", _evidence.Html(reason), "skip"));

    public void PassTest(string message) => _test.Pass(_evidence.Build("Scenario passed", _evidence.Html(message)));

    public void FailTest(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _test.Fail(exception);
    }

    public void SkipTest(string reason) => _test.Skip(_evidence.Html(reason));

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        FlushPendingDetails();
        while (Current is not null)
        {
            PopStep();
        }
    }

    private ExtentTest CurrentOrTest() => Current?.Node ?? _test;

    private void Log(AtfReportStatus status, string message) =>
        CurrentOrTest().Log(ExtentStatusMapper.ToExtent(status), _evidence.Html(message ?? string.Empty));

    internal void FlushPendingDetails()
    {
        var frame = Current;
        if (frame is null || frame.Details.Count == 0)
        {
            return;
        }

        if (ApiScenarioPhaseLayout.Classify(frame.Title) is not null)
        {
            // Lifecycle phases are structural containers. Generic Extent
            // "Execution summary · N fields" rows add noise here and make
            // setup/body/teardown look like executable API steps. Keep
            // diagnostic evidence on the HTTP or validation child instead.
            frame.Details.Clear();
            return;
        }

        var evidence = ExecutionEvidenceComposer.Compose(
            frame.Title,
            frame.Details.Select(detail => new KeyValuePair<string, string>(detail.Key, detail.Value)).ToArray());
        if (evidence.IsGenericSummary)
        {
            // Context fields such as fixture ids and phase combinations are
            // intentionally not promoted to a synthetic Extent event. They
            // can otherwise land on the last HTTP node and look like a second
            // execution result, even though they have no verdict of their own.
            frame.Details.Clear();
            return;
        }

        var body = evidence.IsAssertion
            ? evidence.BodyHtml
            : "<div class='helm-api-classification technical'><span class='helm-api-classification-label'>TECHNICAL TRACE</span></div>" + evidence.BodyHtml;
        frame.Node.Info(_evidence.Build(evidence.Title, body, evidence.Kind));
        frame.Details.Clear();
    }

    internal void PopStep()
    {
        if (_frames.Value is { Count: > 0 } frames)
        {
            frames.Pop();
        }
    }

    private static bool IsHttpStepTitle(string title)
    {
        var verb = title.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
        return verb is "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS" or "TRACE";
    }

    private static bool IsValidationStepTitle(string title) =>
        title.StartsWith("Validate:", StringComparison.OrdinalIgnoreCase);

    private bool TryGetCurrentPhase(out StepFrame phase)
    {
        if (_frames.Value is { Count: > 0 } frames)
        {
            phase = frames.FirstOrDefault(frame => ApiScenarioPhaseLayout.Classify(frame.Title) is not null)!;
            if (phase is not null)
            {
                return true;
            }
        }

        phase = null!;
        return false;
    }

    public sealed class StepFrame
    {
        internal StepFrame(ExtentTest node, string title)
        {
            Node = node;
            Title = title;
        }

        public ExtentTest Node { get; }
        public string Title { get; }
        public List<(string Key, string Value)> Details { get; } = [];
    }
}
