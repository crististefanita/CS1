using Atf.Core.Reporting;
using AtfExtentTestScope = Atf.Reporting.Extent.ExtentTestScope;
using AventStack.ExtentReports;

namespace Atf.Reporting.Extent.Tests;

public sealed class ExtentScopeTreeCoordinatorTests
{
    [Fact(DisplayName = "Starting a new lifecycle phase closes the previous phase tree")]
    public void StartingNewPhaseClosesPreviousPhaseTree()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("ApiTests").CreateNode("lifecycle");
        using var scope = new AtfExtentTestScope(scenario, "ApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.Setup);
        scope.StartStep("GET /setup");
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.StartStep(ApiScenarioPhaseLayout.TestBody);

        Assert.Equal(
            [ApiScenarioPhaseLayout.Setup, ApiScenarioPhaseLayout.TestBody],
            scenario.Model.Children.Select(child => child.Name).ToArray());
        Assert.Single(scenario.Model.Children[0].Children);
    }

    [Fact(DisplayName = "Validation cannot start outside an active request")]
    public void ValidationRequiresAnActiveRequest()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("ApiTests").CreateNode("lifecycle");
        using var scope = new AtfExtentTestScope(scenario, "ApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            scope.StartStep("Validate: response contract"));

        Assert.Contains("active HTTP request", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Completing a phase drains an unclosed request without orphaning the tree")]
    public void CompletingPhaseDrainsUnclosedRequest()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("ApiTests").CreateNode("lifecycle");
        using var scope = new AtfExtentTestScope(scenario, "ApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/users");
        scope.Detail("method", "GET");
        scope.CompletePhase(ApiScenarioPhaseLayout.TestBody, AtfReportStatus.Fail);
        scope.CompleteScenario(AtfReportStatus.Fail);

        var phase = Assert.Single(scenario.Model.Children);
        var request = Assert.Single(phase.Children);
        Assert.Equal("GET /public/v2/users", request.Name);
        Assert.Contains(request.Logs, log => log.Details.Contains("request-details", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Concurrent completion and dispose do not duplicate the scope tree")]
    public async Task ConcurrentCompletionAndDisposeAreSerialized()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("ApiTests").CreateNode("concurrent lifecycle");
        var scope = new AtfExtentTestScope(scenario, "ApiTests");
        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/users");
        scope.Detail("response.status", "200");

        using var barrier = new Barrier(2);
        var tasks = new[]
        {
            Task.Run(() =>
            {
                Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                scope.CompleteScenario(AtfReportStatus.Fail);
            }, TestContext.Current.CancellationToken),
            Task.Run(() =>
            {
                Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                scope.Dispose();
            }, TestContext.Current.CancellationToken)
        };

        await Task.WhenAll(tasks);
        scope.Dispose();

        var phase = Assert.Single(scenario.Model.Children);
        var request = Assert.Single(phase.Children);
        Assert.Equal("GET /public/v2/users", request.Name);
        Assert.Single(request.Logs, log => log.Details.Contains("response.status", StringComparison.Ordinal));
    }
}
