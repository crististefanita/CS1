using Atf.Core.Reporting;
using System.Runtime.ExceptionServices;

namespace Atf.Api.Infrastructure;

internal static class ApiScenarioPhaseRunner
{
    public static async Task RunAsync(
        IAtfScenarioScope scope,
        string phase,
        ApiScenarioContext context,
        Func<ApiScenarioContext, Task> action)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);

        // A renderer is a diagnostic side effect. A broken StartStep must not
        // prevent the actual phase from running or replace its outcome.
        TryReport(() => scope.StartStep(phase));

        Exception? failure = null;
        ApiScenarioSkipException? skip = null;
        try
        {
            await action(context);
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
            if (failure is not null)
            {
                TryReport(() => scope.Fail($"{phase} failed.", failure));
                TryReport(() => scope.CompletePhase(phase, AtfReportStatus.Fail));
            }
            else if (skip is not null)
            {
                TryReport(() => scope.Skip(skip.Message));
                TryReport(() => scope.CompletePhase(phase, AtfReportStatus.Skip));
            }
            else
            {
                TryReport(() => scope.CompletePhase(phase, AtfReportStatus.Pass));
            }
        }

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        if (skip is not null)
        {
            ExceptionDispatchInfo.Capture(skip).Throw();
        }
    }

    private static void TryReport(Action publication)
    {
        try
        {
            publication();
        }
        catch
        {
            // The phase action owns the functional outcome. A renderer failure
            // may reduce evidence, but it must never change that outcome.
        }
    }
}
