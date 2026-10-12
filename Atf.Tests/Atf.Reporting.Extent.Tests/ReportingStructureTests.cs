using Atf.Core.Reporting;
using AtfExtentTestScope = Atf.Reporting.Extent.ExtentTestScope;
using AventStack.ExtentReports;

namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingStructureTests
{
    [Fact(DisplayName = "Extent lifecycle phases contain request-validation sibling pairs")]
    public void LifecyclePhasesContainRequestValidationSiblingPairs()
    {
        var reports = new ExtentReports();
        var classNode = reports.CreateTest("PostsApiTests");
        var scenario = classNode.CreateNode("POSTS API — setup → test → teardown");
        using var scope = new AtfExtentTestScope(scenario, "PostsApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.Setup);
        scope.StartStep("GET /public/v2/posts");
        scope.Info("HTTP exchange completed.");
        scope.StartStep("Validate: setup status=200");
        scope.AssertionTable([new AtfAssertion("HTTP status", "200", "200", true)]);
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
        var setupChildren = scenario.Model.Children[0].Children;
        Assert.Equal(
            ["GET /public/v2/posts", "Validate: setup status=200"],
            setupChildren.Select(child => child.Name).ToArray());
        var setupRequest = setupChildren[0];
        Assert.Equal("GET /public/v2/posts", setupRequest.Name);
        var setupValidation = setupChildren[1];
        Assert.Equal("Validate: setup status=200", setupValidation.Name);
        Assert.Empty(setupValidation.Children);
        Assert.Empty(scenario.Model.Children[1].Children);
        Assert.Empty(scenario.Model.Children[2].Children);
    }

    [Fact(DisplayName = "Extent evidence is attached to the active API step")]
    public void EvidenceStaysWithActiveStep()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("UsersApiTests").CreateNode("GET users");
        using var scope = new AtfExtentTestScope(scenario, "UsersApiTests");

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
        using var scope = new AtfExtentTestScope(scenario, "PostsApiTests");

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
        using var scope = new AtfExtentTestScope(scenario, "PostsApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.Setup);
        scope.StartStep("GET /public/v2/posts");
        scope.Detail("method", "GET");
        scope.Detail("response.status", "200");
        scope.Detail("combination.resource", "posts");
        scope.Detail("combination.phaseShape", "setup → test → teardown");
        scope.StartStep("Validate: setup status=200");
        scope.AssertionTable([new AtfAssertion("HTTP status", "200", "200", true)]);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompletePhase(ApiScenarioPhaseLayout.Setup, AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

        var phase = Assert.Single(scenario.Model.Children);
        Assert.Equal(
            ["GET /public/v2/posts", "Validate: setup status=200"],
            phase.Children.Select(child => child.Name).ToArray());
        var request = phase.Children[0];
        Assert.Equal("GET /public/v2/posts", request.Name);
        var validation = phase.Children[1];
        Assert.Equal("Validate: setup status=200", validation.Name);
        Assert.Contains(request.Logs, log => log.Details.Contains("Response details", StringComparison.Ordinal));
        Assert.DoesNotContain(request.Logs, log => log.Details.Contains("Execution summary", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(request.Logs, log => log.Details.Contains("COMBINATION RESOURCE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Validation evidence follows the API request as a phase sibling")]
    public void ValidationIsGroupedBesideRequest()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("PostsApiTests").CreateNode("GET posts");
        using var scope = new AtfExtentTestScope(scenario, "PostsApiTests");

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
        var validation = phase.Children[1];
        Assert.Equal("Validate: status=404", validation.Name);
        Assert.Empty(validation.Children);
        Assert.Contains(request.Logs, log => log.Details.Contains("Response details", StringComparison.Ordinal));
        Assert.Contains(validation.Logs, log => log.Details.Contains("assertion-evidence", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Report path resolver preserves an absolute path")]
    public void ReportPathResolverPreservesAbsolutePath()
    {
        var resolver = new ReportPathResolver(currentDirectory: Path.GetTempPath(), baseDirectory: Path.GetTempPath());
        var absolute = Path.Combine(Path.GetTempPath(), "atf", "report.html");

        Assert.Equal(Path.GetFullPath(absolute), resolver.Resolve(absolute));
    }
}
