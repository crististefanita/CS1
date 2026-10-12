using AventStack.ExtentReports;
using Atf.Core.Security;
using System.Net;

namespace Atf.Reporting.Extent;

/// <summary>
/// Scenario-scoped adapter over Extent. It owns the execution tree and keeps
/// report evidence attached to the currently active API step.
/// </summary>
public sealed class ExtentTestScope : IAtfScenarioScope
{
    private readonly ExtentTest _test;
    private readonly string _testClassName;
    private readonly Action<string, AtfReportStatus> _recordScenarioStatus;
    private readonly SensitiveDataRedactor _redactor;
    private readonly object _sync;
    private readonly AsyncLocal<Stack<StepFrame>?> _frames = new();
    private int _disposed;

    public ExtentTestScope(
        ExtentTest test,
        string testClassName,
        int assertionMaxDisplayCharacters = ReportingDefaults.AssertionMaxDisplayCharacters)
        : this(test, testClassName, static (_, _) => { }, assertionMaxDisplayCharacters, null, null)
    {
    }

    public ExtentTestScope(
        ExtentTest test,
        string testClassName,
        Action<string, AtfReportStatus> recordScenarioStatus,
        int assertionMaxDisplayCharacters = ReportingDefaults.AssertionMaxDisplayCharacters,
        SensitiveDataRedactor? redactor = null,
        object? synchronizationGate = null)
    {
        _test = test ?? throw new ArgumentNullException(nameof(test));
        ArgumentException.ThrowIfNullOrWhiteSpace(testClassName);
        ArgumentNullException.ThrowIfNull(recordScenarioStatus);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assertionMaxDisplayCharacters);

        _testClassName = testClassName;
        _recordScenarioStatus = recordScenarioStatus;
        _redactor = redactor ?? new SensitiveDataRedactor();
        _sync = synchronizationGate ?? new object();
        AssertionMaxDisplayCharacters = assertionMaxDisplayCharacters;
    }

    public int AssertionMaxDisplayCharacters { get; }

    public StepFrame? Current
    {
        get
        {
            lock (_sync)
            {
                return _frames.Value is { Count: > 0 } frames ? frames.Peek() : null;
            }
        }
    }

    public void CompleteScenario(AtfReportStatus status)
    {
        lock (_sync)
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
    }

    public void StartStep(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        lock (_sync)
        {
            // Lifecycle phases are the direct children of a scenario. HTTP
            // requests and their aggregate validations are direct siblings
            // under the phase, matching the Extent report model.
            if (ApiScenarioPhaseLayout.Classify(title) is not null)
            {
                while (Current is not null)
                {
                    FlushPendingDetails();
                    PopStep();
                }
            }
            else if (IsHttpStepTitle(title))
            {
                // A defensive boundary for callers that start a new exchange
                // without explicitly closing a previous non-phase step.
                CloseActiveNonPhaseStep();
            }
            else if (IsValidationStepTitle(title))
            {
                if (!TryGetCurrentPhase(out _) || Current is null || !IsHttpStepTitle(Current.Title))
                {
                    throw new InvalidOperationException(
                        $"Validation '{title}' must start after an active HTTP request inside a lifecycle phase.");
                }

                // Close the request before creating validation so both remain
                // direct children of the same lifecycle phase. The response
                // ownership is preserved by the ordered request/validation pair
                // and the one-table validation invariant.
                FlushPendingDetails();
                PopStep();
            }

            var node = CurrentOrTest().CreateNode(WebUtility.HtmlEncode(title), string.Empty);
            (_frames.Value ??= new Stack<StepFrame>()).Push(new StepFrame(node, title));
        }
    }

    public void CompletePhase(string phase, AtfReportStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);

        lock (_sync)
        {
            if (status == AtfReportStatus.Pass && _frames.Value is { Count: > 0 } frames)
            {
                var phaseFrame = frames.FirstOrDefault(frame =>
                    string.Equals(frame.Title, phase, StringComparison.Ordinal));
                if (phaseFrame is not null)
                {
                    EnsureSuccessfulBodyHasContractValidation(phaseFrame);
                }
            }

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
    }

    public void CompleteStep(AtfReportStatus status)
    {
        lock (_sync)
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
    }

    public void Detail(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_sync)
        {
            var frame = Current;
            if (frame is null)
            {
                _test.Info(EvidenceTableFormatter.BuildTechnicalTraceTableHtml(
                    [new KeyValuePair<string, string>(key, _redactor.RedactField(key, value ?? string.Empty))]));
                return;
            }

            frame.Details.Add((key, _redactor.RedactField(key, value ?? string.Empty)));
        }
    }

    public void Json(string label, string json)
    {
        lock (_sync)
        {
            FlushPendingDetails();
            var safeJson = _redactor.RedactJson(json);
            var jsonLabel = ExtentEvidenceRenderer.JsonDisplayLabel(label);
            var body = $"<div class='helm-scroll-block'><div class='helm-scroll-block-title'>{ExtentEvidenceRenderer.Html(jsonLabel)}</div>" +
                       $"<div class='helm-scroll-block-body'><pre>{ExtentEvidenceRenderer.Html(safeJson)}</pre></div></div>";
            CurrentOrTest().Info(ExtentEvidenceRenderer.Build(jsonLabel, body, "json-evidence"));
        }
    }

    public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);
        lock (_sync)
        {
            var frame = Current;
            if (frame is null || !IsValidationStepTitle(frame.Title))
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
    }

    public void Info(string message) => Log(AtfReportStatus.Info, message);

    public void Pass(string message) => Log(AtfReportStatus.Pass, message);

    public void Fail(string message, Exception? exception = null)
    {
        lock (_sync)
        {
            var detail = exception is null
                ? _redactor.RedactText(message)
                : $"{_redactor.RedactText(message)}<pre>{ExtentEvidenceRenderer.Html(_redactor.RedactText(exception.ToString()))}</pre>";
            CurrentOrTest().Fail(ExtentEvidenceRenderer.Build("Failure", detail, "fail"));
        }
    }

    public void Warning(string message) => Log(AtfReportStatus.Warning, message);

    public void Skip(string reason)
    {
        lock (_sync)
        {
            CurrentOrTest().Skip(ExtentEvidenceRenderer.Build("Skipped", ExtentEvidenceRenderer.Html(_redactor.RedactText(reason)), "skip"));
        }
    }

    public void PassTest(string message)
    {
        lock (_sync)
        {
            _test.Pass(ExtentEvidenceRenderer.Build("Scenario passed", ExtentEvidenceRenderer.Html(_redactor.RedactText(message))));
        }
    }

    public void FailTest(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        lock (_sync)
        {
            _test.Fail(new InvalidOperationException(_redactor.RedactText(exception.ToString())));
        }
    }

    public void SkipTest(string reason)
    {
        lock (_sync)
        {
            _test.Skip(ExtentEvidenceRenderer.Html(_redactor.RedactText(reason)));
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        lock (_sync)
        {
            FlushPendingDetails();
            while (Current is not null)
            {
                PopStep();
            }
        }
    }

    private ExtentTest CurrentOrTest() => Current?.Node ?? _test;

    private void Log(AtfReportStatus status, string message)
    {
        lock (_sync)
        {
            CurrentOrTest().Log(ExtentStatusMapper.ToExtent(status), ExtentEvidenceRenderer.Html(_redactor.RedactText(message ?? string.Empty)));
        }
    }

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
            // diagnostic evidence on the HTTP request or its validation sibling.
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
        frame.Node.Info(ExtentEvidenceRenderer.Build(evidence.Title, body, evidence.Kind));
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
        var verb = title.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return verb is "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS" or "TRACE";
    }

    private static bool IsValidationStepTitle(string title) =>
        title.StartsWith("Validate:", StringComparison.OrdinalIgnoreCase);

    private static void EnsureSuccessfulBodyHasContractValidation(StepFrame phase)
    {
        var children = phase.Node.Model.Children.ToArray();
        for (var index = 0; index < children.Length; index++)
        {
            var request = children[index];
            if (!IsHttpStepTitle(request.Name))
            {
                continue;
            }

            if (request.Children.Any(child => IsValidationStepTitle(child.Name)))
            {
                throw new InvalidOperationException(
                    $"API request '{request.Name}' contains a nested contract validation. " +
                    "Validation must be its direct sibling under the lifecycle phase.");
            }

            if (index + 1 >= children.Length || !IsValidationStepTitle(children[index + 1].Name))
            {
                throw new InvalidOperationException(
                    $"API request '{request.Name}' completed without contract validation. " +
                    "Every successful response must be followed by exactly one aggregate ValidationTable sibling.");
            }

            var validation = children[index + 1];
            var assertionEvidenceCount = validation.Logs.Count(log =>
                log.Details.Contains("assertion-evidence", StringComparison.Ordinal));
            if (assertionEvidenceCount != 1)
            {
                throw new InvalidOperationException(
                    $"Validation '{validation.Name}' must publish exactly one aggregate AssertionTable.");
            }

            index++;
        }
    }

    private void CloseActiveNonPhaseStep()
    {
        while (Current is not null && ApiScenarioPhaseLayout.Classify(Current.Title) is null)
        {
            FlushPendingDetails();
            Current.Node.Model.Status = ExtentStatusMapper.ToExtent(AtfReportStatus.Info);
            PopStep();
        }
    }

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

        internal int AssertionTableCount { get; set; }
    }
}
