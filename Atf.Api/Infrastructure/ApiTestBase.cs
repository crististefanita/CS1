using Atf.Common.Lifecycle;
using Atf.Common.Modules;
using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Runtime.ExceptionServices;
using Xunit;
using Xunit.Sdk;
using Atf.Api.Infrastructure.Lifecycle;
using Atf.Api.Infrastructure.Outcome;

namespace Atf.Api.Infrastructure;

public abstract class ApiTestBase
{
    private readonly ITestOutputHelper _output;

    protected ApiTestBase(ITestOutputHelper output, IAtfApiReportSession fixture)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        Fixture = RegisterTestAssembly(fixture ?? throw new ArgumentNullException(nameof(fixture)));
    }

    protected IAtfApiReportSession Fixture { get; }
    protected Atf.Core.Configuration.AtfConfiguration Configuration => Fixture.Configuration;

    private IAtfApiReportSession RegisterTestAssembly(IAtfApiReportSession fixture)
    {
        fixture.RegisterTestAssembly(GetType().Assembly);
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
        using var module = ApiModuleFactory.FromSettings(Configuration.Api, scope);
        var context = new ApiScenarioContext(
            Configuration,
            scope,
            module.Users,
            module.Posts,
            new CleanupRegistry(scope));

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            context,
            setup,
            body,
            teardown,
            exception => _output.WriteLine($"Teardown infrastructure failure: {exception}"));

        // The report test is the xUnit class. Each invocation is a scenario
        // node under that class; SETUP / TEST BODY / TEARDOWN remain beneath
        // the scenario and are not promoted into the left-hand test list.
        switch (result.Outcome)
        {
            case ApiScenarioOutcome.Skipped skipped:
                scope.CompleteScenario(AtfReportStatus.Skip);
                scope.SkipTest(skipped.Reason + " " + skipped.Exception.Message);
                // xUnit v3 carries the dynamic-skip reason to the runner. This is
                // deliberately thrown only after the Extent node was marked Skip,
                // keeping both execution authorities aligned.
                throw SkipException.ForSkip(skipped.Exception.Message);

            case ApiScenarioOutcome.Failed failed:
                scope.CompleteScenario(AtfReportStatus.Fail);
                scope.FailTest(failed.Exception);
                ExceptionDispatchInfo.Capture(failed.Exception).Throw();
                break;

            case ApiScenarioOutcome.Warning warning:
                scope.CompleteScenario(AtfReportStatus.Warning);
                scope.PassTest(warning.Reason);
                break;

            default:
                scope.CompleteScenario(AtfReportStatus.Pass);
                scope.PassTest("Scenario passed.");
                break;
        }
    }

    protected static void RequireWriteToken(ApiScenarioContext context)
    {
        if (!context.Configuration.Api.WriteTestsEnabled)
        {
            throw new ApiScenarioSkipException("Write tests are disabled by Api:WriteTestsEnabled=false.");
        }

        var variable = context.Configuration.Api.TokenEnvironmentVariable;
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)))
        {
            throw new ApiScenarioSkipException($"Write test requires environment variable {variable}; no secret is stored in the project.");
        }
    }
}
