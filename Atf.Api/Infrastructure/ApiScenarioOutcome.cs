namespace Atf.Api.Infrastructure.Outcome;

public abstract record ApiScenarioOutcome
{
    public sealed record Passed : ApiScenarioOutcome;

    public sealed record Failed(string Reason, Exception Exception) : ApiScenarioOutcome;

    public sealed record Skipped(string Reason, Exception Exception) : ApiScenarioOutcome;

    public sealed record Warning(string Reason) : ApiScenarioOutcome;
}

public sealed record ApiScenarioExecutionResult(ApiScenarioOutcome Outcome)
{
    public bool IsPassed => Outcome is ApiScenarioOutcome.Passed;
    public bool IsFailed => Outcome is ApiScenarioOutcome.Failed;
    public bool IsSkipped => Outcome is ApiScenarioOutcome.Skipped;
    public bool IsWarning => Outcome is ApiScenarioOutcome.Warning;

    public Exception? Failure => (Outcome as ApiScenarioOutcome.Failed)?.Exception;
    public Exception? Skip => (Outcome as ApiScenarioOutcome.Skipped)?.Exception;
    public string? WarningReason => (Outcome as ApiScenarioOutcome.Warning)?.Reason;
}

/// <summary>
/// Closed semantic result for teardown. Keeping the outcome discriminated
/// prevents callers from interpreting two nullable exceptions inconsistently.
/// </summary>
internal abstract record ApiScenarioTeardownOutcome
{
    public sealed record Passed : ApiScenarioTeardownOutcome;

    public sealed record Failed(Exception Exception) : ApiScenarioTeardownOutcome;

    public sealed record Skipped(Exception Exception) : ApiScenarioTeardownOutcome;
}

internal sealed record ApiScenarioTeardownResult(ApiScenarioTeardownOutcome Outcome)
{
    public bool IsFailed => Outcome is ApiScenarioTeardownOutcome.Failed;
}

internal static class ApiScenarioOutcomeResolver
{
    public static ApiScenarioOutcome Resolve(
        Exception? primaryFailure,
        Exception? skip,
        Exception? teardownFailure,
        bool hasWarnings)
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

        if (hasWarnings)
        {
            return new ApiScenarioOutcome.Warning("Scenario completed with warnings.");
        }

        return new ApiScenarioOutcome.Passed();
    }
}

public sealed class ApiScenarioSkipException(string message) : Exception(message);
