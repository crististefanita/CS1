using Atf.Core.Reporting;
using Atf.Api.Infrastructure;
using Atf.Api.Infrastructure.Outcome;

namespace Atf.Api.Infrastructure.Lifecycle;

/// <summary>
/// Coordinates the scenario lifecycle. Phase execution, teardown safety and
/// outcome precedence are separate seams so each policy can be tested without
/// constructing the complete acceptance host.
/// </summary>
public sealed class ApiScenarioLifecycleRunner
{
    public static async Task<ApiScenarioExecutionResult> RunAsync(
        IAtfScenarioScope scope,
        ApiScenarioContext context,
        Func<ApiScenarioContext, Task>? setup,
        Func<ApiScenarioContext, Task> body,
        Func<ApiScenarioContext, Task>? teardown,
        Action<Exception>? onTeardownInfrastructureFailure = null)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(body);

        Exception? failure = null;
        Exception? skip = null;
        Exception? teardownFailure = null;

        try
        {
            if (setup is not null)
            {
                await ApiScenarioPhaseRunner.RunAsync(scope, ApiScenarioPhaseLayout.Setup, context, setup);
            }

            await ApiScenarioPhaseRunner.RunAsync(scope, ApiScenarioPhaseLayout.TestBody, context, body);
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
            if (teardown is not null || context.Cleanup.HasPending)
            {
                var teardownResult = await ApiScenarioTeardownRunner.RunAsync(scope, context, teardown);
                if (failure is null && teardownResult.Outcome is ApiScenarioTeardownOutcome.Skipped skipped)
                {
                    skip ??= skipped.Exception;
                }

                if (teardownResult.Outcome is ApiScenarioTeardownOutcome.Failed failed)
                {
                    teardownFailure = failed.Exception;
                }

                if (teardownFailure is not null)
                {
                    NotifyTeardownInfrastructureFailure(scope, onTeardownInfrastructureFailure, teardownFailure);
                }
            }
        }

        return new(ApiScenarioOutcomeResolver.Resolve(failure, skip, teardownFailure, scope.HasWarnings));
    }

    private static void NotifyTeardownInfrastructureFailure(
        IAtfScenarioScope scope,
        Action<Exception>? callback,
        Exception exception)
    {
        if (callback is null)
        {
            return;
        }

        try
        {
            callback(exception);
        }
        catch (Exception callbackException)
        {
            try
            {
                scope.Warning($"Teardown failure callback failed: {callbackException.Message}");
            }
            catch
            {
                // Both paths are diagnostic; the teardown exception remains
                // the authoritative lifecycle outcome.
            }
        }
    }
}

