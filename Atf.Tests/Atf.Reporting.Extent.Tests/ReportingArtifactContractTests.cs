using System.Text.RegularExpressions;

namespace Atf.Reporting.Extent.Tests;

using System.Text.RegularExpressions;

public sealed class ReportingArtifactContractTests
{
    [Fact(DisplayName = "Final HTML keeps the request-validation sibling hierarchy")]
    public void FinalHtmlPreservesRequestValidationSiblingHierarchy()
    {
        var root = Directory.CreateTempSubdirectory("atf-rendered-hierarchy-");
        try
        {
            var reportPath = Path.Combine(root.FullName, "api-atf-report.html");
            var lifecycle = new ExtentReportLifecycle();
            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = reportPath,
                TimestampEnabled = false,
                Engine = "extent"
            });

            using (var scope = lifecycle.StartTest(
                       "UsersApiTests",
                       "GET users",
                       "sibling validation contract",
                       ["api", "contract"]))
            {
                scope.StartStep(ApiScenarioPhaseLayout.TestBody);
                scope.StartStep("GET /public/v2/users");
                scope.Detail("response.status", "200");
                scope.Detail("response.duration.ms", "42");
                scope.Json("RESPONSE JSON", "{\"id\":1}");
                scope.StartStep("Validate: users collection contract");
                scope.AssertionTable([
                    new AtfAssertion("HTTP status", "200 OK", "200 OK", true)
                ]);
                scope.CompleteStep(AtfReportStatus.Pass);
                scope.CompletePhase(ApiScenarioPhaseLayout.TestBody, AtfReportStatus.Pass);
                scope.CompleteScenario(AtfReportStatus.Pass);
            }

            lifecycle.FinalizeReport();

            var actualReportPath = lifecycle.ReportPath;
            var inspection = ReportArtifactInspector.Inspect(actualReportPath);
            Assert.True(inspection.IsCompliant, string.Join(", ", inspection.Violations));
            Assert.True(inspection.Satisfies("response-owned-execution-hierarchy"));

            var html = File.ReadAllText(actualReportPath);
            Assert.Contains("GET /public/v2/users", html, StringComparison.Ordinal);
            Assert.Contains("Validate: users collection contract", html, StringComparison.Ordinal);
            Assert.Contains(
                "validationCard.setAttribute('data-atf-parent-node', 'phase')",
                html,
                StringComparison.Ordinal);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Final HTML keeps class navigation free of scenario tag annotations")]
    public void FinalHtmlKeepsClassNavigationFreeOfScenarioTags()
    {
        var root = Directory.CreateTempSubdirectory("atf-rendered-navigation-");
        try
        {
            var reportPath = Path.Combine(root.FullName, "api-atf-report.html");
            var lifecycle = new ExtentReportLifecycle();
            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = reportPath,
                TimestampEnabled = false,
                Engine = "extent"
            });

            using (var scope = lifecycle.StartTest(
                       "UsersApiTests",
                       "GET users",
                       "scenario tags stay in the detail projection",
                       ["api", "contract", "users"]))
            {
                scope.StartStep(ApiScenarioPhaseLayout.TestBody);
                scope.StartStep("GET /public/v2/users");
                scope.Detail("response.status", "200");
                scope.Json("RESPONSE JSON", "{\"id\":1}");
                scope.StartStep("Validate: users collection contract");
                scope.AssertionTable([
                    new AtfAssertion("HTTP status", "200 OK", "200 OK", true)
                ]);
                scope.CompleteStep(AtfReportStatus.Pass);
                scope.CompletePhase(ApiScenarioPhaseLayout.TestBody, AtfReportStatus.Pass);
                scope.CompleteScenario(AtfReportStatus.Pass);
            }

            lifecycle.FinalizeReport();

            var html = File.ReadAllText(lifecycle.ReportPath);
            Assert.DoesNotContain("class=\"atf-test-annotations\"", html, StringComparison.Ordinal);
            Assert.DoesNotContain("class='atf-test-annotations'", html, StringComparison.Ordinal);
            Assert.Contains("atf-scenario-annotations", html, StringComparison.Ordinal);
            Assert.Contains("function configureScenarioTagAnnotations()", html, StringComparison.Ordinal);

            var classItems = Regex.Matches(
                    html,
                    "<li\\s+class=\\\"test-item\\\"(?:(?!>).)*\\btag=\\\"(?<tag>[^\\\"]*)\\\"",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .ToArray();

            Assert.NotEmpty(classItems);
            Assert.All(classItems, item => Assert.Empty(item.Groups["tag"].Value));
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }
}
