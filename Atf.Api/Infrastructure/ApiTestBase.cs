using Atf.Common.Lifecycle;
using Atf.Common.Modules;
using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Runtime.ExceptionServices;
using Xunit;
using Xunit.Sdk;

namespace Atf.Api.Infrastructure;

public abstract class ApiTestBase(ITestOutputHelper output, ExtentCollectionFixture fixture)
{
    private sealed class ScenarioSkipException(string message) : Exception(message);

    protected ExtentCollectionFixture Fixture { get; } = RegisterTestAssembly(fixture);
    protected Atf.Core.Configuration.AtfConfiguration Configuration => Fixture.Configuration;

    private static ExtentCollectionFixture RegisterTestAssembly(ExtentCollectionFixture fixture)
    {
        fixture.RegisterTestAssembly(typeof(ApiTestBase).Assembly);
        return fixture;
    }

    protected async Task RunScenarioAsync(
        string name,
        IEnumerable<string> categories,
        Func<ApiScenarioContext, Task> body)
        => await RunScenarioAsync(name, categories, setup: null, body, teardown: null);

    protected async Task RunScenarioAsync(
        string name,
        IEnumerable<string> categories,
        Func<ApiScenarioContext, Task>? setup,
        Func<ApiScenarioContext, Task> body,
        Func<ApiScenarioContext, Task>? teardown = null)
    {
        using var scope = Fixture.StartTest(GetType().Name, name, string.Empty, categories);
        using var module = new ApiModule(Configuration.Api, scope);
        var context = new ApiScenarioContext(
            Configuration,
            scope,
            module.Users,
            module.Posts,
            new CleanupRegistry(scope));

        Exception? failure = null;
        Exception? skip = null;
        try
        {
            if (setup is not null)
            {
                await RunPhaseAsync(scope, ApiScenarioPhaseLayout.Setup, context, setup);
            }

            await RunPhaseAsync(scope, ApiScenarioPhaseLayout.TestBody, context, body);
        }
        catch (ScenarioSkipException exception)
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
                try
                {
                    await RunPhaseAsync(
                        scope,
                        ApiScenarioPhaseLayout.Teardown,
                        context,
                        async teardownContext =>
                        {
                            if (teardown is not null)
                            {
                                await teardown(teardownContext);
                            }

                            await teardownContext.Cleanup.RunAsync();
                        });
                }
                catch (ScenarioSkipException exception)
                {
                    if (failure is null && skip is null)
                    {
                        skip = exception;
                    }
                }
                catch (Exception teardownException)
                {
                    output.WriteLine($"Teardown infrastructure failure: {teardownException}");
                    if (failure is null && skip is null)
                    {
                        failure = teardownException;
                    }
                }
            }
        }

        // The Extent test is the xUnit class. Each invocation is a scenario
        // node under that class; SETUP / TEST BODY / TEARDOWN remain beneath
        // the scenario and are not promoted into the left-hand test list.
        scope.CompleteScenario(failure is null && skip is null
            ? AtfReportStatus.Pass
            : failure is not null
                ? AtfReportStatus.Fail
                : AtfReportStatus.Skip);

        if (skip is not null)
        {
            scope.SkipTest(skip.Message);
            // xUnit v3 carries the dynamic-skip reason to the runner. This is
            // deliberately thrown only after the Extent node was marked Skip,
            // keeping both execution authorities aligned.
            throw SkipException.ForSkip(skip.Message);
        }

        if (failure is not null)
        {
            scope.FailTest(failure);
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        scope.PassTest("Scenario passed.");
    }

    private static async Task RunPhaseAsync(
        ExtentTestScope scope,
        string phase,
        ApiScenarioContext context,
        Func<ApiScenarioContext, Task> action)
    {
        scope.StartStep(phase);
        try
        {
            await action(context);
            scope.CompletePhase(phase, AtfReportStatus.Pass);
        }
        catch (ScenarioSkipException exception)
        {
            scope.Skip(exception.Message);
            scope.CompletePhase(phase, AtfReportStatus.Skip);
            throw;
        }
        catch (Exception exception)
        {
            scope.Fail($"{phase} failed.", exception);
            scope.CompletePhase(phase, AtfReportStatus.Fail);
            throw;
        }
    }

    protected static void RequireWriteToken(ApiScenarioContext context)
    {
        if (!context.Configuration.Api.WriteTestsEnabled)
        {
            throw new ScenarioSkipException("Write tests are disabled by Api:WriteTestsEnabled=false.");
        }

        var variable = context.Configuration.Api.TokenEnvironmentVariable;
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)))
        {
            throw new ScenarioSkipException($"Write test requires environment variable {variable}; no secret is stored in the project.");
        }
    }
}
