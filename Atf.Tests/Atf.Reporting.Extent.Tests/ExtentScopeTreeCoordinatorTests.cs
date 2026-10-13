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
}
