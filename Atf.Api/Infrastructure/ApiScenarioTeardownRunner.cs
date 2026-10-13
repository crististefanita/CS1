using Atf.Core.Reporting;

namespace Atf.Api.Infrastructure;

internal static class ApiScenarioTeardownRunner
{
    public static async Task<TeardownExecutionResult> RunAsync(
        IAtfScenarioScope scope,
        ApiScenarioContext context,
        Func<ApiScenarioContext, Task>? teardown)
    {
        Exception? failure = null;
        Exception? skip = null;

        // The phase can still execute and cleanup can still be owned safely if
        // the renderer cannot open its visual shell. This is deliberately not
        // part of the semantic teardown result.
        var phaseStarted = TryReport(() => scope.StartStep(ApiScenarioPhaseLayout.Teardown));

        try
        {
            if (teardown is not null)
            {
                await teardown(context);
            }
        }
        catch (ApiScenarioSkipException exception)
        {
            skip = exception;
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            try
            {
                await context.Cleanup.RunAsync();
            }
            catch (Exception cleanupException)
            {
                if (failure is null && skip is null)
                {
                    failure = cleanupException;
                }
                else
                {
                    TryWarning(scope, $"Cleanup infrastructure failed: {cleanupException.Message}");
                }
            }
        }

        if (phaseStarted)
        {
            if (failure is not null)
            {
                TryReport(() => scope.Fail($"{ApiScenarioPhaseLayout.Teardown} failed.", failure));
                TryReport(() => scope.CompletePhase(ApiScenarioPhaseLayout.Teardown, AtfReportStatus.Fail));
            }
            else if (skip is not null)
            {
                TryReport(() => scope.Skip(skip.Message));
                TryReport(() => scope.CompletePhase(ApiScenarioPhaseLayout.Teardown, AtfReportStatus.Skip));
            }
            else
            {
                TryReport(() => scope.CompletePhase(ApiScenarioPhaseLayout.Teardown, AtfReportStatus.Pass));
            }
        }

        return new(failure, skip);
    }

    private static bool TryReport(Action publication)
    {
        try
        {
            publication();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TryWarning(IAtfScenarioScope scope, string message)
    {
        try
        {
            scope.Warning(message);
        }
        catch
        {
            // Reporting must not block cleanup or replace the primary result.
        }
    }
}

internal sealed record TeardownExecutionResult(Exception? Failure, Exception? Skip);
