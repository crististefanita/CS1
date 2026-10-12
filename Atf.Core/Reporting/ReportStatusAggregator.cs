using AventStack.ExtentReports;

namespace Atf.Core.Reporting;

/// <summary>
/// Thread-safe status aggregation for a logical test class. A class is failed
/// if any scenario fails; otherwise it is skipped when at least one scenario is
/// skipped; otherwise it is passed.
/// </summary>
public sealed class ReportStatusAggregator
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Status> _statuses = new(StringComparer.Ordinal);

    public Status Record(string logicalTestName, Status next)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalTestName);

        lock (_sync)
        {
            var aggregate = _statuses.TryGetValue(logicalTestName, out var current)
                ? Merge(current, next)
                : next;
            _statuses[logicalTestName] = aggregate;
            return aggregate;
        }
    }

    public bool TryGet(string logicalTestName, out Status status)
    {
        lock (_sync)
        {
            return _statuses.TryGetValue(logicalTestName, out status);
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _statuses.Clear();
        }
    }

    private static Status Merge(Status current, Status next)
    {
        if (current == Status.Fail || next == Status.Fail)
        {
            return Status.Fail;
        }

        if (current == Status.Skip || next == Status.Skip)
        {
            return Status.Skip;
        }

        return Status.Pass;
    }
}
