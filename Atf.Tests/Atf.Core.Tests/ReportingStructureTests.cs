using Atf.Core.Reporting;
using AventStack.ExtentReports;

namespace Atf.Core.Tests;

public sealed class ReportingStructureTests
{
    [Fact(DisplayName = "Extent lifecycle phases are direct siblings under the scenario")]
    public void LifecyclePhasesAreScenarioSiblings()
    {
        var reports = new ExtentReports();
        var classNode = reports.CreateTest("PostsApiTests");
        var scenario = classNode.CreateNode("POSTS API — setup → test → teardown");
        using var scope = new ExtentTestScope(scenario, "PostsApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.Setup);
        scope.StartStep("GET /public/v2/posts");
        scope.Info("HTTP exchange completed.");
        scope.CompleteStep(Status.Pass);
        scope.CompleteStep(Status.Pass);
        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.CompleteStep(Status.Pass);
        scope.StartStep(ApiScenarioPhaseLayout.Teardown);
        scope.CompleteStep(Status.Pass);
        scope.CompleteScenario(Status.Pass);

        Assert.Equal(
            ["SETUP", "TEST BODY", "TEARDOWN"],
            scenario.Model.Children.Select(child => child.Name).ToArray());
        Assert.Equal("GET /public/v2/posts", scenario.Model.Children[0].Children.Single().Name);
        Assert.Empty(scenario.Model.Children[1].Children);
        Assert.Empty(scenario.Model.Children[2].Children);
    }

    [Fact(DisplayName = "Extent evidence is attached to the active API step")]
    public void EvidenceStaysWithActiveStep()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("UsersApiTests").CreateNode("GET users");
        using var scope = new ExtentTestScope(scenario, "UsersApiTests");

        scope.StartStep("GET /public/v2/users");
        scope.Detail("method", "GET");
        scope.Detail("url", "https://example.test/users");
        scope.Detail("response.status", "200");
        scope.Json("response.body", "{\"id\":1}");
        scope.CompleteStep(Status.Pass);

        var step = Assert.Single(scenario.Model.Children);
        Assert.Contains(step.Logs, log => log.Details.Contains("Response details", StringComparison.Ordinal));
        Assert.Contains(step.Logs, log => log.Details.Contains("RESPONSE JSON", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Report path resolver preserves an absolute path")]
    public void ReportPathResolverPreservesAbsolutePath()
    {
        var resolver = new ReportPathResolver(currentDirectory: Path.GetTempPath(), baseDirectory: Path.GetTempPath());
        var absolute = Path.Combine(Path.GetTempPath(), "atf", "report.html");

        Assert.Equal(Path.GetFullPath(absolute), resolver.Resolve(absolute));
    }
}
