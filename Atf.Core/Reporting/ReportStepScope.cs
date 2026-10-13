using Atf.Core.Reporting.Ports;

namespace Atf.Core.Reporting.Lifecycle;

/// <summary>
/// Renderer-neutral disposable lifecycle guard for one report step. It gives
/// orchestration code a safe default while still allowing an explicit status
/// before disposal.
/// </summary>
public sealed class ReportStepScope : IDisposable
{
    private readonly IAtfLifecycleReporter _reporter;
    private AtfReportStatus _status = AtfReportStatus.Info;
    private int _completed;

    public ReportStepScope(IAtfLifecycleReporter reporter, string title)
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        _reporter.StartStep(title);
    }

    public void Complete(AtfReportStatus status)
    {
        _status = status;
        if (Interlocked.Exchange(ref _completed, 1) == 0)
        {
            _reporter.CompleteStep(status);
        }
    }

    public void Dispose() => Complete(_status);
}
