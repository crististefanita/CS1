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
        scope.CompleteStep(Status.Pass);

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.CompleteStep(Status.Pass);

        scope.StartStep(ApiScenarioPhaseLayout.Teardown);
        scope.CompleteStep(Status.Pass);
        scope.CompleteScenario(Status.Pass);

        Assert.Equal(
            ["SETUP", "TEST BODY", "TEARDOWN"],
            scenario.Model.Children.Select(child => child.Name).ToArray());
        Assert.Single(scenario.Model.Children[0].Children);
        Assert.Empty(scenario.Model.Children[1].Children);
        Assert.Empty(scenario.Model.Children[2].Children);
    }
}
