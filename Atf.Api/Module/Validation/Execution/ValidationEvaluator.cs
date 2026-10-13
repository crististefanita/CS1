namespace Atf.Common.Validation.Execution;

/// <summary>
/// Evaluates one validation group without knowing anything about reporting.
/// The runner/publisher consumes the immutable result afterwards, so a
/// reporter cannot influence validity or exception precedence.
/// </summary>
internal static class ValidationEvaluator
{
    public static ValidationEvaluation Evaluate(Action<ValidationTable> configure, int maxDisplayedCharacters)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var table = new ValidationTable(maxDisplayedCharacters);
        try
        {
            configure(table);
        }
        catch (Exception exception) when (!table.IsFinished)
        {
            TryRecordUnexpectedFailure(table, exception);
            table.MarkFinished();
            return new ValidationEvaluation(
                table.Rows.ToArray(),
                HasFailures: true,
                Failure: exception,
                UnexpectedException: exception);
        }

        if (!table.HasAssertions)
        {
            table.AddMissingAssertionRow();
        }

        var failure = table.HasFailures ? table.FirstFailure() : null;
        table.MarkFinished();
        return new ValidationEvaluation(
            table.Rows.ToArray(),
            table.HasFailures,
            failure,
            UnexpectedException: null);
    }

    private static void TryRecordUnexpectedFailure(ValidationTable table, Exception exception)
    {
        try
        {
            table.AddFailure("Validation execution", "no unexpected exception", exception.Message, exception);
        }
        catch (Exception)
        {
            // The original configuration exception remains authoritative.
        }
    }
}
