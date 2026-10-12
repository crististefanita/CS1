using AventStack.ExtentReports;
using System.Collections.Concurrent;
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
    private readonly AsyncLocal<Stack<StepFrame>?> _frames = new();
    private readonly ExtentEvidenceRenderer _evidence = new();
    private int _disposed;

    internal ExtentTestScope(
        ExtentTest test,
        string testClassName,
        int assertionMaxDisplayCharacters = ReportingDefaults.AssertionMaxDisplayCharacters)
    {
        _test = test ?? throw new ArgumentNullException(nameof(test));
        ArgumentException.ThrowIfNullOrWhiteSpace(testClassName);
        if (assertionMaxDisplayCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assertionMaxDisplayCharacters));
        }

        _testClassName = testClassName;
        AssertionMaxDisplayCharacters = assertionMaxDisplayCharacters;
    }

    public int AssertionMaxDisplayCharacters { get; }

    public StepFrame? Current => _frames.Value is { Count: > 0 } frames ? frames.Peek() : null;

    public void CompleteScenario(Status status)
    {
        FlushPendingDetails();
        while (Current is not null)
        {
            Current.Node.Model.Status = status;
            PopStep();
        }

        _test.Model.Status = status;
        ExtentManager.RecordScenarioStatus(_testClassName, status);
    }

    public void StartStep(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        // Lifecycle phases are the direct children of a scenario. An API
        // exchange may be nested below SETUP/TEST BODY/TEARDOWN, but the next
        // phase must close the previous phase first.
        if (ApiScenarioPhaseLayout.Classify(title) is not null)
        {
            while (Current is not null)
            {
                FlushPendingDetails();
                PopStep();
            }
        }

        var node = CurrentOrTest().CreateNode(WebUtility.HtmlEncode(title), string.Empty);
        (_frames.Value ??= new Stack<StepFrame>()).Push(new StepFrame(node, title));
    }

    public void CompleteStep(Status status)
    {
        var frame = Current;
        if (frame is null)
        {
            return;
        }

        FlushPendingDetails();
        frame.Node.Model.Status = status;
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

    public void Info(string message) => Log(Status.Info, message);

    public void Pass(string message) => Log(Status.Pass, message);

    public void Fail(string message, Exception? exception = null)
    {
        var detail = exception is null
            ? message
            : $"{message}<pre>{_evidence.Html(exception.ToString())}</pre>";
        CurrentOrTest().Fail(_evidence.Build("Failure", detail, "fail"));
    }

    public void Warning(string message) => Log(Status.Warning, message);

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

    private void Log(Status status, string message) =>
        CurrentOrTest().Log(status, _evidence.Html(message ?? string.Empty));

    internal void FlushPendingDetails()
    {
        var frame = Current;
        if (frame is null || frame.Details.Count == 0)
        {
            return;
        }

        var evidence = ExecutionEvidenceComposer.Compose(
            frame.Title,
            frame.Details.Select(detail => new KeyValuePair<string, string>(detail.Key, detail.Value)).ToArray());
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
