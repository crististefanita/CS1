using Atf.Common.Validation.Execution;

namespace Atf.Common.Tests;

public sealed class ValidationEvaluatorTests
{
    [Fact(DisplayName = "Evaluator returns one immutable aggregate result without a reporter")]
    public void EvaluatorProducesNeutralAggregateResult()
    {
        var result = ValidationEvaluator.Evaluate(table =>
        {
            table.Add("HTTP status", "200", "200", passed: true);
            table.Add("Content-Type", "application/json", "application/json", passed: true);
        }, maxDisplayedCharacters: 240);

        Assert.False(result.IsUnexpectedFailure);
        Assert.False(result.HasFailures);
        Assert.Null(result.Failure);
        Assert.Equal(["HTTP status", "Content-Type"], result.Rows.Select(row => row.Name).ToArray());
    }

    [Fact(DisplayName = "Evaluator preserves partial rows and the original exception")]
    public void EvaluatorPreservesPartialRowsOnUnexpectedFailure()
    {
        var expected = new InvalidOperationException("configuration exploded");

        var result = ValidationEvaluator.Evaluate(table =>
        {
            table.Add("HTTP status", "200", "200", passed: true);
            throw expected;
        }, maxDisplayedCharacters: 240);

        Assert.True(result.IsUnexpectedFailure);
        Assert.Same(expected, result.UnexpectedException);
        Assert.Same(expected, result.Failure);
        Assert.Equal(
            ["HTTP status", "Validation execution"],
            result.Rows.Select(row => row.Name).ToArray());
    }
}
