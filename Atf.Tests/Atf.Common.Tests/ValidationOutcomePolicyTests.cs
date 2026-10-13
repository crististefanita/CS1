using Atf.Common.Validation.Execution;
using Atf.Core.Reporting;

namespace Atf.Common.Tests;

public sealed class ValidationOutcomePolicyTests
{
    [Fact(DisplayName = "Outcome policy keeps hard assertion failures authoritative")]
    public void HardFailureProducesFailAndThrow()
    {
        var failure = new InvalidOperationException("contract mismatch");
        var evaluation = new ValidationEvaluation(
            [new AtfAssertion("HTTP status", "200", "500", false, AtfReportStatus.Fail)],
            HasFailures: true,
            Failure: failure,
            UnexpectedException: null);

        var decision = ValidationOutcomePolicy.Decide(evaluation, warningMode: false);

        Assert.Equal(AtfReportStatus.Fail, decision.Status);
        Assert.True(decision.ShouldThrow);
        Assert.Same(failure, decision.Failure);
        Assert.Equal(AtfReportStatus.Fail, decision.Rows[0].ResultStatus);
    }

    [Fact(DisplayName = "Outcome policy maps only failed warning rows to Warning")]
    public void WarningKeepsRowsVisibleWithoutThrowing()
    {
        var evaluation = new ValidationEvaluation(
            [
                new AtfAssertion("HTTP status", "200", "200", true),
                new AtfAssertion("JSON schema", "matched", "mismatch", false)
            ],
            HasFailures: true,
            Failure: new InvalidOperationException("mismatch"),
            UnexpectedException: null);

        var decision = ValidationOutcomePolicy.Decide(evaluation, warningMode: true);

        Assert.Equal(AtfReportStatus.Warning, decision.Status);
        Assert.False(decision.ShouldThrow);
        Assert.Equal(
            [AtfReportStatus.Pass, AtfReportStatus.Warning],
            decision.Rows.Select(row => row.ResultStatus).ToArray());
    }

    [Fact(DisplayName = "Outcome policy always throws an unexpected evaluator exception")]
    public void UnexpectedFailureCannotBecomeWarning()
    {
        var failure = new InvalidOperationException("configuration error");
        var evaluation = new ValidationEvaluation(
            [new AtfAssertion("Validation execution", "none", "error", false, AtfReportStatus.Fail)],
            HasFailures: true,
            Failure: failure,
            UnexpectedException: failure);

        var decision = ValidationOutcomePolicy.Decide(evaluation, warningMode: true);

        Assert.Equal(AtfReportStatus.Fail, decision.Status);
        Assert.True(decision.ShouldThrow);
        Assert.Same(failure, decision.Failure);
    }
}
