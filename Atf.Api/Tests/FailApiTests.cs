using Atf.Api.Infrastructure;
using Atf.Api.TestHost;
using Atf.Common.Validation;
using System.Net;
using Xunit;
using Xunit.Sdk;

namespace Atf.Api;

/// <summary>
/// Opt-in failure fixtures used to visually verify the Extent failure contract.
/// They are skipped by default so a normal API run remains a trustworthy signal.
/// </summary>
[Collection("API ATF")]
public sealed class FailApiTests(ITestOutputHelper output, ApiTestFixture fixture)
    : ApiTestBase(output, fixture)
{
    [Fact(DisplayName = "Failure demo: setup exception")]
    [Trait("Category", "failure-demo")]
    public Task SetupFailure() => RunFailureScenarioAsync(
        "FAIL DEMO — setup exception",
        setup: static _ => throw new InvalidOperationException("Intentional setup failure for report rendering."),
        body: static _ => Task.CompletedTask);

    [Fact(DisplayName = "Failure demo: contract assertion")]
    [Trait("Category", "failure-demo")]
    public Task ContractAssertionFailure() => RunFailureScenarioAsync(
        "FAIL DEMO — contract assertion",
        body: async context =>
        {
            var result = await context.Users.ListAsync(authProfile: "anonymous");
            ApiAssertions.Status(context.Reporter, result.Response, HttpStatusCode.Created);
        });

    [Fact(DisplayName = "Failure demo: direct assertion")]
    [Trait("Category", "failure-demo")]
    public Task DirectAssertionFailure() => RunFailureScenarioAsync(
        "FAIL DEMO — direct assertion",
        body: static _ =>
        {
            Assert.Fail("Intentional direct xUnit assertion for report rendering.");
            return Task.CompletedTask;
        });

    [Fact(DisplayName = "Failure demo: teardown exception")]
    [Trait("Category", "failure-demo")]
    public Task TeardownFailure() => RunFailureScenarioAsync(
        "FAIL DEMO — teardown exception",
        body: static _ => Task.CompletedTask,
        teardown: static _ => throw new InvalidOperationException("Intentional teardown failure for report rendering."));

    private Task RunFailureScenarioAsync(
        string name,
        Func<ApiScenarioContext, Task>? setup = null,
        Func<ApiScenarioContext, Task>? body = null,
        Func<ApiScenarioContext, Task>? teardown = null)
    {
        RequireFailureDemos();
        return RunScenarioAsync(
            name,
            ["api", "failure-demo", "reporting"],
            setup,
            body ?? (_ => Task.CompletedTask),
            teardown);
    }

    private static void RequireFailureDemos()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("ATF_ENABLE_FAILURE_DEMOS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            throw SkipException.ForSkip(
                "Failure demos are opt-in; set ATF_ENABLE_FAILURE_DEMOS=true to render intentional failures.");
        }
    }
}
