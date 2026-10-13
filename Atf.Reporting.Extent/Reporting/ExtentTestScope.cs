using AventStack.ExtentReports;
using Atf.Core.Security;
using System.Diagnostics.CodeAnalysis;

namespace Atf.Reporting.Extent;

/// <summary>
/// Scenario-scoped adapter over Extent. It owns the execution tree and keeps
/// report evidence attached to the currently active API step.
/// </summary>
public sealed class ExtentTestScope : IAtfScenarioScope
{
#pragma warning disable CA1859 // Port-typed fields are intentional seams for renderer composition.
    private readonly ExtentNodeAdapter _root;
    private readonly string _testClassName;
    private readonly Action<string, AtfReportStatus> _recordScenarioStatus;
    private readonly object _sync;
    private readonly ExtentScopeFrameStack _frames = new();
    [SuppressMessage("Performance", "CA1859", Justification = "The interface is the renderer-independent evidence port.")]
    private readonly IReportEvidencePublisher _evidence;
    [SuppressMessage("Performance", "CA1859", Justification = "The interface is the renderer-independent status-policy port.")]
    private readonly IReportStatusPolicy _statusPolicy;
    private readonly IPhaseTreeWriter _tree;
    private int _disposed;
#pragma warning restore CA1859

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
        _root = new ExtentNodeAdapter(test ?? throw new ArgumentNullException(nameof(test)));
        ArgumentException.ThrowIfNullOrWhiteSpace(testClassName);
        ArgumentNullException.ThrowIfNull(recordScenarioStatus);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assertionMaxDisplayCharacters);

        _testClassName = testClassName;
        _recordScenarioStatus = recordScenarioStatus;
        var safeRedactor = redactor ?? new SensitiveDataRedactor();
        _sync = synchronizationGate ?? new object();
        AssertionMaxDisplayCharacters = assertionMaxDisplayCharacters;
        _statusPolicy = new ExtentScopeStatusPolicy();
        _evidence = new ExtentScopeEvidenceWriter(_root, () => Current, AssertionMaxDisplayCharacters, safeRedactor);
        _tree = new ExtentScopeTreeCoordinator(_root, _frames, _statusPolicy, FlushPendingDetails, PopStep);
    }

    public int AssertionMaxDisplayCharacters { get; }

    public bool HasWarnings
    {
        get
        {
            lock (_sync)
            {
                return _statusPolicy.HasWarnings;
            }
        }
    }

    internal ScenarioStepFrame? Current
    {
        get
        {
            lock (_sync)
            {
                return _tree.Current;
            }
        }
    }

    public void CompleteScenario(AtfReportStatus status)
    {
        lock (_sync)
        {
            var effectiveStatus = _statusPolicy.ResolveScenarioStatus(status);

            _tree.CompleteAll(effectiveStatus);

            _root.SetStatus(ExtentStatusMapper.ToExtent(effectiveStatus));
            _recordScenarioStatus(_testClassName, effectiveStatus);
        }
    }

    public void StartStep(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        lock (_sync)
        {
            _tree.StartStep(title);
        }
    }

    public void CompletePhase(string phase, AtfReportStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);

        lock (_sync)
        {
            _tree.CompletePhase(phase, status);
        }
    }

    public void CompleteStep(AtfReportStatus status)
    {
        lock (_sync)
        {
            _tree.CompleteStep(status);
        }
    }

    public void Detail(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_sync)
        {
            _evidence.Detail(key, value);
        }
    }

    public void Json(string label, string json)
    {
        lock (_sync)
        {
            _evidence.Json(label, json);
        }
    }

    public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);
        lock (_sync)
        {
            _evidence.AssertionTable(assertions);
        }
    }

    public void Info(string message) => Log(AtfReportStatus.Info, message);

    public void Pass(string message) => Log(AtfReportStatus.Pass, message);

    public void Fail(string message, Exception? exception = null)
    {
        lock (_sync)
        {
            _evidence.Fail(message, exception);
        }
    }

    public void Warning(string message) => Log(AtfReportStatus.Warning, message);

    public void Skip(string reason)
    {
        lock (_sync)
        {
            _evidence.Skip(reason);
        }
    }

    public void PassTest(string message)
    {
        lock (_sync)
        {
            if (_statusPolicy.HasWarnings)
            {
                _root.Warning(ExtentEvidenceRenderer.Build(
                    "Scenario completed with warnings",
                    ExtentEvidenceRenderer.Html(message ?? string.Empty),
                    "warning"));
            }
            else
            {
                _evidence.PassTest(message);
            }
        }
    }

    public void FailTest(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        lock (_sync)
        {
            _evidence.FailTest(exception);
        }
    }

    public void SkipTest(string reason)
    {
        lock (_sync)
        {
            _evidence.SkipTest(reason);
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
            _tree.Drain();
        }
    }

    private void Log(AtfReportStatus status, string message)
    {
        lock (_sync)
        {
            if (status == AtfReportStatus.Warning)
            {
                var phase = _frames.Find(frame => ExtentStepClassifier.IsPhase(frame.Title));
                _statusPolicy.RecordWarning(phase?.Title);
            }

            _evidence.Log(status, message);
        }
    }

    internal void FlushPendingDetails()
    {
        _evidence.FlushPendingDetails();
    }

    internal void PopStep()
    {
        _frames.Pop();
    }

}
