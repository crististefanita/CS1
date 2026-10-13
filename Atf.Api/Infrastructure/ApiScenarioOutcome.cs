namespace Atf.Api.Infrastructure;

public abstract record ApiScenarioOutcome
{
    public sealed record Passed : ApiScenarioOutcome;

    public sealed record Failed(string Reason, Exception Exception) : ApiScenarioOutcome;

    public sealed record Skipped(string Reason, Exception Exception) : ApiScenarioOutcome;
}

public sealed record ApiScenarioExecutionResult(ApiScenarioOutcome Outcome)
{
    public bool IsPassed => Outcome is ApiScenarioOutcome.Passed;
    public bool IsFailed => Outcome is ApiScenarioOutcome.Failed;
    public bool IsSkipped => Outcome is ApiScenarioOutcome.Skipped;

    public Exception? Failure => (Outcome as ApiScenarioOutcome.Failed)?.Exception;
    public Exception? Skip => (Outcome as ApiScenarioOutcome.Skipped)?.Exception;
}

internal static class ApiScenarioOutcomeResolver
{
    public static ApiScenarioOutcome Resolve(
        Exception? primaryFailure,
        Exception? skip,
        Exception? teardownFailure)
    {
        if (primaryFailure is not null)
        {
            return new ApiScenarioOutcome.Failed("Scenario execution failed.", primaryFailure);
        }

        if (teardownFailure is not null)
        {
            return new ApiScenarioOutcome.Failed("Teardown failed.", teardownFailure);
        }

        if (skip is not null)
        {
            return new ApiScenarioOutcome.Skipped("Scenario was skipped.", skip);
        }

        return new ApiScenarioOutcome.Passed();
    }
}

public sealed class ApiScenarioSkipException(string message) : Exception(message);
