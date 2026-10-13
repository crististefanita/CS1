using AventStack.ExtentReports;
using Atf.Core.Reporting;
using Atf.Core.Reporting.Evidence;
using Atf.Core.Reporting.Phases;
using Atf.Reporting.Extent;
using Atf.Reporting.Extent.Theme;
using Xunit;

namespace Atf.Reporting.Extent.Tests;

public sealed class ExtentAdapterBoundaryTests
{
    [Fact(DisplayName = "Extent implementation is owned by the adapter assembly")]
    public void ExtentImplementationDoesNotRemainInCore()
    {
        var coreExtentTypes = typeof(IAtfReporter).Assembly
            .GetTypes()
            .Where(type => type.Name.Contains("Extent", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(coreExtentTypes);
        Assert.Equal(typeof(ExtentTestScope).Assembly, typeof(AtfReportTheme).Assembly);
    }

    [Fact(DisplayName = "Adapter keeps the Extent dependency outside renderer-neutral ports")]
    public void AdapterDependsOnCorePortsButCoreDoesNotDependOnExtentPackage()
    {
        var coreReferencesExtent = typeof(IAtfReporter).Assembly
            .GetReferencedAssemblies()
            .Any(reference => reference.Name?.Contains("Extent", StringComparison.OrdinalIgnoreCase) == true);

        Assert.False(coreReferencesExtent);
        Assert.Contains(
            typeof(ExtentTestScope).Assembly.GetReferencedAssemblies(),
            reference => string.Equals(reference.Name, "ExtentReports", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Scenario step frame keeps the Extent vendor behind an adapter")]
    public void StepFrameDoesNotExposeExtentTest()
    {
        var extentTestType = typeof(ExtentTest);
        var frameProperties = typeof(ExtentTestScope.StepFrame).GetProperties(
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        Assert.DoesNotContain(frameProperties, property =>
            extentTestType.IsAssignableFrom(property.PropertyType));
        Assert.Contains(frameProperties, property => property.Name == "Adapter");
    }

    [Fact(DisplayName = "Client facade composes independent navigation and disclosure controllers")]
    public void ClientBehaviourComposesControllersWithoutUnresolvedPlaceholders()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("function configureNavigationOrder()", script, StringComparison.Ordinal);
        Assert.Contains("function installCardDelegation()", script, StringComparison.Ordinal);
        Assert.Contains("function installNavigation()", script, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Request and aggregate validation are ordered siblings under TEST BODY")]
    public void ValidationIsGroupedBesideTheRequestThatProducedTheResponse()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("Users API").CreateNode("TEST BODY");
        using var scope = new ExtentTestScope(scenario, "UsersApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/users");
        scope.Info("HTTP exchange completed; contract validation pending.");
        scope.StartStep("Validate: user collection contract");
        scope.AssertionTable(
        [
            new AtfAssertion("HTTP status", "200 OK", "200 OK", true),
            new AtfAssertion("Content-Type", "application/json", "application/json", true),
            new AtfAssertion("JSON schema", "matched", "matched", true)
        ]);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompleteScenario(AtfReportStatus.Pass);

        var testBody = scenario.Model.Children.Single();
        Assert.Equal(
            ["GET /public/v2/users", "Validate: user collection contract"],
            testBody.Children.Select(child => child.Name).ToArray());
        var request = testBody.Children[0];
        Assert.Equal("GET /public/v2/users", request.Name);
        var validation = testBody.Children[1];
        Assert.Equal("Validate: user collection contract", validation.Name);
        Assert.Contains(validation.Logs, log => log.Details.Contains("assertion-evidence", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Multiple API exchanges preserve request-validation sibling order")]
    public void MultipleRequestsKeepValidationBesideTheirOwningRequests()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("Users API").CreateNode("multiple requests");
        using var scope = new ExtentTestScope(scenario, "UsersApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/users");
        scope.StartStep("Validate: users collection contract");
        scope.AssertionTable([new AtfAssertion("HTTP status", "200", "200", true)]);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.StartStep("GET /public/v2/users/1");
        scope.StartStep("Validate: user item contract");
        scope.AssertionTable([new AtfAssertion("HTTP status", "200", "200", true)]);
        scope.CompleteStep(AtfReportStatus.Pass);
        scope.CompletePhase(ApiScenarioPhaseLayout.TestBody, AtfReportStatus.Pass);

        var testBody = scenario.Model.Children.Single();
        Assert.Equal(
            ["GET /public/v2/users", "Validate: users collection contract", "GET /public/v2/users/1", "Validate: user item contract"],
            testBody.Children.Select(child => child.Name).ToArray());
        Assert.All(testBody.Children.Where(child => child.Name.StartsWith("GET ", StringComparison.Ordinal)), request => Assert.Empty(request.Children));
    }

    [Fact(DisplayName = "Client hierarchy marks validation as a phase sibling")]
    public void ClientBehaviourKeepsValidationBesideRequest()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("canonical tree is phase -> request + validation sibling", script, StringComparison.Ordinal);
        Assert.DoesNotContain("const requestAccordion = directChild(stepCard, 'accordion')", script, StringComparison.Ordinal);
        Assert.Contains("validationCard.setAttribute('data-atf-node-role', 'validation')", script, StringComparison.Ordinal);
        Assert.Contains("validationCard.setAttribute('data-atf-parent-node', 'phase')", script, StringComparison.Ordinal);
        Assert.Contains("data-atf-parent-node', 'phase'", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "A successful API phase rejects a request without contract validation")]
    public void SuccessfulPhaseRequiresValidationOwnership()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("Users API").CreateNode("TEST BODY");
        using var scope = new ExtentTestScope(scenario, "UsersApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/users");
        scope.Info("HTTP exchange completed; contract validation pending.");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            scope.CompletePhase("TEST BODY", AtfReportStatus.Pass));

        Assert.Contains("completed without contract validation", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "A failed request keeps its failure evidence without inventing validation")]
    public void FailedRequestPreservesFailureOwnership()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("Users API").CreateNode("TEST BODY");
        using var scope = new ExtentTestScope(scenario, "UsersApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/users");
        scope.Detail("response.duration.ms", "42");
        scope.Fail("HTTP exchange failed", new InvalidOperationException("synthetic failure"));
        scope.CompleteStep(AtfReportStatus.Fail);
        scope.CompletePhase(ApiScenarioPhaseLayout.TestBody, AtfReportStatus.Fail);
        scope.CompleteScenario(AtfReportStatus.Fail);

        var phase = Assert.Single(scenario.Model.Children);
        var request = Assert.Single(phase.Children);
        Assert.Equal("GET /public/v2/users", request.Name);
        Assert.Empty(request.Children);
        Assert.Contains(request.Logs, log => log.Details.Contains("Failure", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "A validation step must publish exactly one aggregate assertion table")]
    public void ValidationMustPublishExactlyOneAggregateTable()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("Users API").CreateNode("TEST BODY");
        using var scope = new ExtentTestScope(scenario, "UsersApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/users");
        scope.StartStep("Validate: users collection contract");
        scope.AssertionTable([new AtfAssertion("HTTP status", "200", "200", true)]);

        var duplicate = Assert.Throws<InvalidOperationException>(() =>
            scope.AssertionTable([new AtfAssertion("Content-Type", "json", "json", true)]));

        Assert.Contains("already owns", duplicate.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Assertions cannot be published directly on an HTTP request")]
    public void AssertionsCannotBePublishedOnRequestNode()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("Users API").CreateNode("TEST BODY");
        using var scope = new ExtentTestScope(scenario, "UsersApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/users");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            scope.AssertionTable([new AtfAssertion("HTTP status", "200", "200", true)]));

        Assert.Contains("active 'Validate:' step", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "A validation node without its aggregate table cannot close a successful phase")]
    public void ValidationWithoutAggregateTableCannotClosePhase()
    {
        var reports = new ExtentReports();
        var scenario = reports.CreateTest("Users API").CreateNode("TEST BODY");
        using var scope = new ExtentTestScope(scenario, "UsersApiTests");

        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
        scope.StartStep("GET /public/v2/users");
        scope.StartStep("Validate: users collection contract");
        scope.CompleteStep(AtfReportStatus.Pass);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            scope.CompletePhase(ApiScenarioPhaseLayout.TestBody, AtfReportStatus.Pass));

        Assert.Contains("exactly one aggregate AssertionTable", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Theme keeps JSON evidence vertically scrollable without horizontal overflow")]
    public void ThemePreservesEvidenceLayoutContract()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains("overflow-y: auto", css, StringComparison.Ordinal);
        Assert.Contains("overflow-x: hidden", css, StringComparison.Ordinal);
        Assert.Contains("white-space: pre-wrap", css, StringComparison.Ordinal);
        Assert.Contains("word-break: break-word", css, StringComparison.Ordinal);
        Assert.Contains("text-overflow: ellipsis", css, StringComparison.Ordinal);
        Assert.Contains("white-space: nowrap", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Composed HTML preserves response-owned validation hierarchy")]
    public void ComposedHtmlPreservesResponseOwnedValidationHierarchy()
    {
        var root = Directory.CreateTempSubdirectory("atf-hierarchy-artifact-");
        try
        {
            var lifecycle = new ExtentReportLifecycle();
            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "api-atf-report.html"),
                TimestampEnabled = false,
                Engine = "extent"
            });

            using (var scope = lifecycle.StartTest(
                       "UsersApiTests",
                       "GET users",
                        "response-owned validation layout",
                       ["api", "contract"]))
            {
                scope.StartStep("TEST BODY");
                scope.StartStep("GET /public/v2/users");
                scope.Detail("method", "GET");
                scope.Detail("response.status", "200");
                scope.Detail("response.duration.ms", "42");
                scope.Json("RESPONSE JSON", "{\"id\":1}");
                scope.StartStep("Validate: users collection contract");
                scope.AssertionTable([
                    new AtfAssertion("HTTP status", "200 OK", "200 OK", true)
                ]);
                scope.CompleteStep(AtfReportStatus.Pass);
                scope.CompletePhase("TEST BODY", AtfReportStatus.Pass);
                scope.CompleteScenario(AtfReportStatus.Pass);
            }

            lifecycle.FinalizeReport();

            var inspection = ReportArtifactInspector.Inspect(lifecycle.ReportPath);
        Assert.True(inspection.Satisfies("response-owned-execution-hierarchy"),
                string.Join(", ", inspection.Violations));
            Assert.True(inspection.Satisfies("self-contained"));
            Assert.False(Directory.Exists(Path.Combine(root.FullName, "extent")));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact(DisplayName = "Evidence disclosure titles stay readable and escape diagnostic text")]
    public void EvidenceDisclosureTitlesAreCompactAndSafe()
    {
        var evidence = ExtentEvidenceRenderer.Build(
            "  Response details · 8 fields <secret>  ",
            "<p>safe body</p>");

        Assert.Contains("class='se-title'", evidence, StringComparison.Ordinal);
        Assert.Contains("title='", evidence, StringComparison.Ordinal);
        Assert.Contains("aria-label='", evidence, StringComparison.Ordinal);
        Assert.Contains("&lt;secret&gt;", evidence, StringComparison.Ordinal);
        Assert.Contains("se-title", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("<secret>", evidence, StringComparison.Ordinal);
    }
}
