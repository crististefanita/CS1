using Atf.Core.Reporting;

namespace Atf.Common.Validation.Execution;

internal sealed record ValidationDecision(
    IReadOnlyList<AtfAssertion> Rows,
    AtfReportStatus Status,
    string Message,
    Exception? Failure,
    bool ShouldThrow)
{
    public bool IsSuccess => Status == AtfReportStatus.Pass;
}

/// <summary>
/// Pure exception/status policy for one evaluated validation group. No
/// reporter, Extent type or HTML concern is allowed here.
/// </summary>
internal static class ValidationOutcomePolicy
{
    public static ValidationDecision Decide(ValidationEvaluation evaluation, bool warningMode)
    {
        ArgumentNullException.ThrowIfNull(evaluation);

        if (evaluation.IsUnexpectedFailure)
        {
            return new(
                evaluation.Rows,
                AtfReportStatus.Fail,
                "Validation failed.",
                evaluation.UnexpectedException,
                ShouldThrow: true);
        }

        if (!evaluation.HasFailures)
        {
            return new(
                evaluation.Rows,
                AtfReportStatus.Pass,
                $"{evaluation.Rows.Count} validations passed.",
                Failure: null,
                ShouldThrow: false);
        }

        if (!warningMode)
        {
            return new(
                evaluation.Rows,
                AtfReportStatus.Fail,
                "Validation failed.",
                evaluation.Failure,
                ShouldThrow: true);
        }

        var warningRows = evaluation.Rows
            .Select(row => row with
            {
                ResultStatus = row.Passed ? AtfReportStatus.Pass : AtfReportStatus.Warning
            })
            .ToArray();

        return new(
            warningRows,
            AtfReportStatus.Warning,
            "Soft validation warning: one or more assertions failed.",
            Failure: null,
            ShouldThrow: false);
    }
}
