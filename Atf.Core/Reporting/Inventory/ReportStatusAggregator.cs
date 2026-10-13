namespace Atf.Core.Reporting.Inventory;

/// <summary>
/// Thread-safe status aggregation for a logical test class. A class is failed
/// if any scenario fails; otherwise it is skipped when at least one scenario is
/// skipped; otherwise a warning is preserved; otherwise it is passed.
/// </summary>
public sealed class ReportStatusAggregator
{
    private readonly object _sync = new();
    private readonly Dictionary<string, AtfReportStatus> _statuses = new(StringComparer.Ordinal);

    public AtfReportStatus Record(string logicalTestName, AtfReportStatus next)
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

    public bool TryGet(string logicalTestName, out AtfReportStatus status)
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

    private static AtfReportStatus Merge(AtfReportStatus current, AtfReportStatus next)
    {
        if (current == AtfReportStatus.Fail || next == AtfReportStatus.Fail)
        {
            return AtfReportStatus.Fail;
        }

        if (current == AtfReportStatus.Skip || next == AtfReportStatus.Skip)
        {
            return AtfReportStatus.Skip;
        }

        if (current == AtfReportStatus.Warning || next == AtfReportStatus.Warning)
        {
            return AtfReportStatus.Warning;
        }

        return AtfReportStatus.Pass;
    }
}
