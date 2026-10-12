using Atf.Core.Reporting;
using AventStack.ExtentReports;
using Xunit;

namespace Atf.Api;

public sealed class ApiScenarioPhaseLayoutTests
{
    [Fact]
    public void Lifecycle_scenario_uses_setup_test_body_teardown_order()
    {
        Assert.Equal(
            ["SETUP", "TEST BODY", "TEARDOWN"],
            ApiScenarioPhaseLayout.OrderedLabels(includeSetup: true, includeTeardown: true));
    }

    [Fact]
    public void Test_only_scenario_does_not_render_empty_lifecycle_phases()
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
    public void Phase_titles_are_classified_without_misclassifying_api_steps(string title, string? expected)
    {
        Assert.Equal(expected, ApiScenarioPhaseLayout.Classify(title));
    }

    [Fact]
    public void Lifecycle_phases_are_siblings_under_the_scenario()
    {
        var reports = new ExtentReports();
        var classTest = reports.CreateTest("ApiLifecycleAndCombinationsTests");
        var scenario = classTest.CreateNode("POSTS API — setup → test → teardown");
        using var scope = new ExtentTestScope(scenario, "ApiLifecycleAndCombinationsTests");

        scope.StartStep(ApiScenarioPhaseLayout.Setup);
        scope.StartStep("GET /public/v2/posts");
        scope.Info("HTTP exchange completed.");
        scope.CompleteStep(AtfReportStatus.Pass);

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.CompleteStep(AtfReportStatus.Pass);

        scope.StartStep(ApiScenarioPhaseLayout.Teardown);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

        Assert.Equal(
            ["SETUP", "TEST BODY", "TEARDOWN"],
            scenario.Model.Children.Select(child => child.Name).ToArray());
        Assert.Single(scenario.Model.Children[0].Children);
        Assert.Empty(scenario.Model.Children[1].Children);
        Assert.Empty(scenario.Model.Children[2].Children);
    }

    [Fact(DisplayName = "One response contract is an aggregate child of its HTTP request")]
    public void Request_owns_one_aggregate_validation()
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
            ["GET /public/v2/users"],
            testBody.Children.Select(child => child.Name).ToArray());

        var request = testBody.Children[0];
        Assert.Equal("GET /public/v2/users", request.Name);
        Assert.Equal(["Validate: user collection contract"], request.Children.Select(child => child.Name).ToArray());
        Assert.Contains("Assertions · 2", request.Children[0].Info.Keys);
    }

    [Fact(DisplayName = "Starting a new lifecycle phase closes the previous phase before creating the sibling")]
    public void StartingNewPhaseClosesPreviousPhase()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("ApiLifecycleAndCombinationsTests")
            .CreateNode("POSTS API — setup → test → teardown");
        using var scope = new ExtentTestScope(scenario, "ApiLifecycleAndCombinationsTests");

        scope.StartStep(ApiScenarioPhaseLayout.Setup);
        scope.StartStep("GET /public/v2/posts");
        scope.CompleteStep(AtfReportStatus.Pass);

        // The scenario runner starts the next phase without requiring the
        // caller to remember to close SETUP explicitly.
        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/posts/1");
        scope.StartStep("Validate: status=200");
        scope.CompleteStep(AtfReportStatus.Pass);

        scope.StartStep(ApiScenarioPhaseLayout.Teardown);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

        Assert.Equal(
            ["SETUP", "TEST BODY", "TEARDOWN"],
            scenario.Model.Children.Select(child => child.Name).ToArray());
        Assert.Equal("GET /public/v2/posts", scenario.Model.Children[0].Children.Single().Name);
        var testBody = scenario.Model.Children[1];
        Assert.Equal("GET /public/v2/posts/1", testBody.Children[0].Name);
        Assert.Equal("Validate: status=200", testBody.Children[0].Children.Single().Name);
        Assert.Empty(scenario.Model.Children[2].Children);
    }
}
