using Atf.Core.Reporting;
using AventStack.ExtentReports;

namespace Atf.Core.Tests.Reporting;

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
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.StartStep(ApiScenarioPhaseLayout.Teardown);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

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
        scope.CompleteStep(AtfReportStatus.Pass);

        var step = Assert.Single(scenario.Model.Children);
        Assert.Contains(step.Logs, log => log.Details.Contains("Response details", StringComparison.Ordinal));
        Assert.Contains(step.Logs, log => log.Details.Contains("RESPONSE JSON", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Lifecycle phases do not render generic execution summary evidence")]
    public void LifecyclePhaseDetailsAreSuppressed()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("PostsApiTests").CreateNode("POSTS API — setup → test → teardown");
        using var scope = new ExtentTestScope(scenario, "PostsApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.Setup);
        scope.Detail("combination.resource", "posts");
        scope.Detail("combination.phaseShape", "setup → test → teardown");
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

        var phase = Assert.Single(scenario.Model.Children);
        Assert.DoesNotContain(
            phase.Logs,
            log => log.Details.Contains("Execution summary", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Context details on the active request do not create a generic execution summary")]
    public void ContextDetailsOnRequestAreSuppressed()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("PostsApiTests").CreateNode("POSTS API — setup → test → teardown");
        using var scope = new ExtentTestScope(scenario, "PostsApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.Setup);
        scope.StartStep("GET /public/v2/posts");
        scope.Detail("method", "GET");
        scope.Detail("response.status", "200");
        scope.Detail("combination.resource", "posts");
        scope.Detail("combination.phaseShape", "setup → test → teardown");
        scope.CompletePhase(ApiScenarioPhaseLayout.Setup, AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

        var request = Assert.Single(Assert.Single(scenario.Model.Children).Children);
        Assert.Contains(request.Logs, log => log.Details.Contains("Response details", StringComparison.Ordinal));
        Assert.DoesNotContain(request.Logs, log => log.Details.Contains("Execution summary", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(request.Logs, log => log.Details.Contains("COMBINATION RESOURCE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Validation evidence is a sibling of the API request under the phase")]
    public void ValidationIsPhaseSiblingOfRequest()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("PostsApiTests").CreateNode("GET posts");
        using var scope = new ExtentTestScope(scenario, "PostsApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/posts/999999999");
        scope.Detail("method", "GET");
        scope.Detail("response.status", "404");
        scope.Info("HTTP exchange completed; contract validation pending.");

        scope.StartStep("Validate: status=404");
        scope.AssertionTable([
            new AtfAssertion("HTTP status", "404 NotFound", "404 NotFound", true)
        ]);
        scope.Pass("1 validations passed.");
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompletePhase(ApiScenarioPhaseLayout.TestBody, AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

        var phase = Assert.Single(scenario.Model.Children);
        Assert.Equal(ApiScenarioPhaseLayout.TestBody, phase.Name);
        Assert.Equal(
            ["GET /public/v2/posts/999999999", "Validate: status=404"],
            phase.Children.Select(child => child.Name).ToArray());

        var request = phase.Children[0];
        Assert.Equal("GET /public/v2/posts/999999999", request.Name);
        Assert.Empty(request.Children);
        var validation = phase.Children[1];
        Assert.Equal("Validate: status=404", validation.Name);
        Assert.Empty(validation.Children);
        Assert.Contains(request.Logs, log => log.Details.Contains("Response details", StringComparison.Ordinal));
        Assert.Contains(validation.Logs, log => log.Details.Contains("Assertions · 1", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Report path resolver preserves an absolute path")]
    public void ReportPathResolverPreservesAbsolutePath()
    {
        var resolver = new ReportPathResolver(currentDirectory: Path.GetTempPath(), baseDirectory: Path.GetTempPath());
        var absolute = Path.Combine(Path.GetTempPath(), "atf", "report.html");

        Assert.Equal(Path.GetFullPath(absolute), resolver.Resolve(absolute));
    }
}
