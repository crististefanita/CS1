using Atf.Core.Reporting;

namespace Atf.Common.Validation.Execution;

/// <summary>
/// Publishes validation evidence as a diagnostic side effect. A renderer is
/// allowed to fail without changing the authoritative validation outcome.
/// </summary>
internal sealed class ValidationReportPublisher(IAtfValidationReporter reporter)
{
    private readonly IAtfValidationReporter _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));

    public int AssertionMaxDisplayCharacters => Try(
        () => _reporter.AssertionMaxDisplayCharacters,
        ReportingDefaults.AssertionMaxDisplayCharacters);

    public void Start(string title) => Try(() => _reporter.StartStep(title));

    public void PublishPass(string message) => Try(() => _reporter.Pass(message));

    public void PublishFail(string message, Exception? exception = null) =>
        Try(() => _reporter.Fail(message, exception));

    public void PublishWarning(string message) => Try(() => _reporter.Warning(message));

    public void PublishTable(IReadOnlyCollection<AtfAssertion> assertions) =>
        Try(() => _reporter.AssertionTable(assertions));

    public void Complete(AtfReportStatus status) => Try(() => _reporter.CompleteStep(status));

    private static void Try(Action action)
    {
        try
        {
            action();
        }
        catch (Exception)
        {
            // Report publication is deliberately non-authoritative. The
            // caller still owns the functional validation result and its
            // exception precedence.
        }
    }

    private static T Try<T>(Func<T> action, T fallback)
    {
        try
        {
            return action();
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
