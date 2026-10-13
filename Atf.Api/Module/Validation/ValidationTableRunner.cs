using Atf.Core.Reporting;

namespace Atf.Common.Validation.Execution;

/// <summary>
/// Owns the execution lifecycle of one aggregate validation while
/// <see cref="ValidationTable"/> remains the assertion model.
/// </summary>
internal sealed class ValidationTableRunner(IAtfValidationReporter reporter)
{
    private readonly ValidationReportPublisher _publisher = new(reporter);

    public void Run(string title, Action<ValidationTable> configure)
        => Run(title, configure, AtfReportStatus.Fail, throwOnAssertionFailure: true);

    public void RunWarning(string title, Action<ValidationTable> configure)
        => Run(title, configure, AtfReportStatus.Warning, throwOnAssertionFailure: false);

    private void Run(
        string title,
        Action<ValidationTable> configure,
        AtfReportStatus assertionFailureStatus,
        bool throwOnAssertionFailure)
    {
        _publisher.Start(title);
        var status = AtfReportStatus.Pass;

        try
        {
            var evaluation = ValidationEvaluator.Evaluate(configure, _publisher.AssertionMaxDisplayCharacters);
            var decision = ValidationOutcomePolicy.Decide(
                evaluation,
                warningMode: assertionFailureStatus == AtfReportStatus.Warning || !throwOnAssertionFailure);
            status = decision.Status;
            _publisher.PublishTable(decision.Rows);

            if (decision.IsSuccess)
            {
                _publisher.PublishPass(decision.Message);
            }
            else if (decision.Status == AtfReportStatus.Warning)
            {
                _publisher.PublishWarning(decision.Message);
            }
            else
            {
                _publisher.PublishFail(decision.Message, decision.Failure);
            }

            if (decision.ShouldThrow)
            {
                throw decision.Failure!;
            }
        }
        catch
        {
            status = AtfReportStatus.Fail;
            throw;
        }
        finally
        {
            // Every validation is a leaf evidence step. Closing it here is
            // essential when several validators follow one request: otherwise
            // Extent attaches the next validation below the previous one.
            _publisher.Complete(status);
        }
    }

}
