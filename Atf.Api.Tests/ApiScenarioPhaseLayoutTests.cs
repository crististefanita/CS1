using Atf.Core.Reporting;
using AventStack.ExtentReports;
using Xunit;

namespace Atf.Api;

public sealed class ApiScenarioPhaseLayoutTests
{
    [Fact]
    public void LifecycleScenarioUsesSetupTestBodyTeardownOrder()
    {
        Assert.Equal(
            ["SETUP", "TEST BODY", "TEARDOWN"],
            ApiScenarioPhaseLayout.OrderedLabels(includeSetup: true, includeTeardown: true));
    }

    [Fact]
    public void TestOnlyScenarioDoesNotRenderEmptyLifecyclePhases()
    {
        Assert.Equal(
            ["TEST BODY"],
            ApiScenarioPhaseLayout.OrderedLabels(includeSetup: false, includeTeardown: false));
    }

    [Theory]
    [InlineData("SETUP", "setup")]
    [InlineData("TEST BODY", "test-body")]
    [InlineData("TEARDOWN", "teardown")]
    [InlineData("GET /public/v2/posts", null)]
    public void PhaseTitlesAreClassifiedWithoutMisclassifyingApiSteps(string title, string? expected)
    {
        Assert.Equal(expected, ApiScenarioPhaseLayout.Classify(title));
    }

    [Fact]
    public void LifecyclePhasesAreSiblingsUnderTheScenario()
    {
        var reports = new ExtentReports();
        var classTest = reports.CreateTest("ApiLifecycleAndCombinationsTests");
        var scenario = classTest.CreateNode("POSTS API — setup → test → teardown");
        using var scope = new ExtentTestScope(scenario, "ApiLifecycleAndCombinationsTests");

        scope.StartStep(ApiScenarioPhaseLayout.Setup);
        scope.StartStep("GET /public/v2/posts");
        scope.Info("HTTP exchange completed.");
        scope.StartStep("Validate: setup status=200");
        scope.AssertionTable(
        [
            new AtfAssertion("HTTP status", "200 OK", "200 OK", true)
        ]);
        scope.CompleteStep(AtfReportStatus.Pass);

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.CompleteStep(AtfReportStatus.Pass);

        scope.StartStep(ApiScenarioPhaseLayout.Teardown);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

        Assert.Equal(
            ["SETUP", "TEST BODY", "TEARDOWN"],
            scenario.Model.Children.Select(child => child.Name).ToArray());
        var setupChildren = scenario.Model.Children[0].Children.ToArray();
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

    [Fact(DisplayName = "One response contract follows its request as a phase sibling")]
    public void ValidationFollowsRequestAsPhaseSibling()
    {
        var reports = new ExtentReports();
        var classTest = reports.CreateTest("ApiLifecycleAndCombinationsTests");
        var scenario = classTest.CreateNode("GET users — collection contract");
        using var scope = new ExtentTestScope(scenario, "ApiLifecycleAndCombinationsTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/users");
        scope.Info("HTTP exchange completed; contract validation pending.");

        scope.StartStep("Validate: user collection contract");
        scope.AssertionTable(
        [
            new AtfAssertion("HTTP status", "200 OK", "200 OK", true),
            new AtfAssertion("JSON schema", "matched", "matched", true)
        ]);
        scope.Pass("2 validations passed.");
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompletePhase(ApiScenarioPhaseLayout.TestBody, AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

        var testBody = scenario.Model.Children.Single();
        Assert.Equal(
            ["GET /public/v2/users", "Validate: user collection contract"],
            testBody.Children.Select(child => child.Name).ToArray());
        var request = testBody.Children[0];
        Assert.Equal("GET /public/v2/users", request.Name);
        var validation = testBody.Children[1];
        Assert.Equal("Validate: user collection contract", validation.Name);
        Assert.Empty(validation.Children);
        Assert.Contains(validation.Logs, log => log.Details.Contains("assertion-evidence", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Starting a new lifecycle phase closes the previous phase before creating the next phase")]
    public void StartingNewPhaseClosesPreviousPhase()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("ApiLifecycleAndCombinationsTests")
            .CreateNode("POSTS API — setup → test → teardown");
        using var scope = new ExtentTestScope(scenario, "ApiLifecycleAndCombinationsTests");

        scope.StartStep(ApiScenarioPhaseLayout.Setup);
        scope.StartStep("GET /public/v2/posts");
        scope.StartStep("Validate: setup status=200");
        scope.AssertionTable(
        [
            new AtfAssertion("HTTP status", "200 OK", "200 OK", true)
        ]);
        scope.CompleteStep(AtfReportStatus.Pass);

        // The scenario runner starts the next phase without requiring the
        // caller to remember to close SETUP explicitly.
        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/posts/1");
        scope.StartStep("Validate: status=200");
        scope.AssertionTable(
        [
            new AtfAssertion("HTTP status", "200 OK", "200 OK", true)
        ]);
        scope.CompleteStep(AtfReportStatus.Pass);

        scope.StartStep(ApiScenarioPhaseLayout.Teardown);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

        Assert.Equal(
            ["SETUP", "TEST BODY", "TEARDOWN"],
            scenario.Model.Children.Select(child => child.Name).ToArray());
        var setupChildren = scenario.Model.Children[0].Children.ToArray();
        Assert.Equal(
            ["GET /public/v2/posts", "Validate: setup status=200"],
            setupChildren.Select(child => child.Name).ToArray());
        var setupRequest = setupChildren[0];
        Assert.Equal("GET /public/v2/posts", setupRequest.Name);
        var setupValidation = setupChildren[1];
        Assert.Equal("Validate: setup status=200", setupValidation.Name);
        Assert.Empty(setupValidation.Children);
        var testBody = scenario.Model.Children[1];
        var testChildren = testBody.Children.ToArray();
        Assert.Equal(
            ["GET /public/v2/posts/1", "Validate: status=200"],
            testChildren.Select(child => child.Name).ToArray());
        var testRequest = testChildren[0];
        Assert.Equal("GET /public/v2/posts/1", testRequest.Name);
        var testValidation = testChildren[1];
        Assert.Equal("Validate: status=200", testValidation.Name);
        Assert.Empty(testValidation.Children);
        Assert.Empty(scenario.Model.Children[2].Children);
    }
}

