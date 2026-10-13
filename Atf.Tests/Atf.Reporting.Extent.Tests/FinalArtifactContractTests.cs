using Atf.Core.Configuration;
using Atf.Core.Reporting;
using Atf.Core.Reporting.Artifacts;
using Atf.Reporting.Extent;

namespace Atf.Reporting.Extent.Tests;

public sealed class FinalArtifactContractTests
{
    [Fact(DisplayName = "Category artifact preserves pass, fail and skip status badges")]
    public void CategoryArtifactPreservesAllStatusVariants()
    {
        var root = Directory.CreateTempSubdirectory("atf-category-status-artifact-");
        try
        {
            var lifecycle = new ExtentReportLifecycle();
            lifecycle.Initialize(new ReportingSettings
            {
                Engine = "extent",
                ReportPath = Path.Combine(root.FullName, "api-report.html"),
                TimestampEnabled = false,
                DocumentTitle = "ATF category status contract",
                ReportName = "ATF category status contract"
            });

            PublishScenario(lifecycle, "CategoryPassTests", "PASS CATEGORY", AtfReportStatus.Pass);
            PublishScenario(lifecycle, "CategoryFailTests", "FAIL CATEGORY", AtfReportStatus.Fail);
            PublishScenario(lifecycle, "CategorySkipTests", "SKIP CATEGORY", AtfReportStatus.Skip);

            lifecycle.FinalizeReport();

            var html = File.ReadAllText(lifecycle.ReportPath);
            var categoryStart = html.IndexOf("<div class=\"test-wrapper row view category-view", StringComparison.Ordinal);
            Assert.True(categoryStart >= 0, "The final artifact does not contain the Category view.");
            var categoryHtml = html[categoryStart..];

            Assert.Contains("<h4>mixed-status</h4>", categoryHtml, StringComparison.Ordinal);
            Assert.Contains("status=\"pass\" class='badge log pass-bg'>1 passed", categoryHtml, StringComparison.Ordinal);
            Assert.Contains("status=\"fail\" class='badge log badge-danger'>1 failed", categoryHtml, StringComparison.Ordinal);
            Assert.Contains("status=\"skip\" class='badge log badge-skip'>1 skipped", categoryHtml, StringComparison.Ordinal);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Final Extent artifact preserves response-owned validation contract")]
    public void FinalHtmlIsInspectableAndSelfContained()
    {
        var root = Directory.CreateTempSubdirectory("atf-final-artifact-");
        try
        {
            var reportPath = Path.Combine(root.FullName, "api-report.html");
            var lifecycle = new ExtentReportLifecycle();
            lifecycle.Initialize(new ReportingSettings
            {
                Engine = "extent",
                ReportPath = reportPath,
                TimestampEnabled = false,
                DocumentTitle = "ATF contract test",
                ReportName = "ATF contract test"
            });

            using (var scenario = lifecycle.StartTest(
                "UsersApiTests",
                "GET users — aggregate contract",
                "offline final artifact contract",
                ["api", "contract"]))
            {
                scenario.StartStep("TEST BODY");
                scenario.StartStep("GET /public/v2/users");
                scenario.Detail("method", "GET");
                scenario.Detail("response.status", "200");
                scenario.Detail("response.duration.ms", "42");
                scenario.Json("response.body", """{"id":1,"name":"demo"}""");
                scenario.StartStep("Validate: users contract");
                scenario.AssertionTable(
                [
                    new AtfAssertion("HTTP status", "200 OK", "200 OK", true),
                    new AtfAssertion("JSONPath id", "exists", "1", true)
                ]);
                scenario.Pass("2 validations passed.");
                scenario.CompleteStep(AtfReportStatus.Pass);
                scenario.CompletePhase("TEST BODY", AtfReportStatus.Pass);
                scenario.CompleteScenario(AtfReportStatus.Pass);
            }

            lifecycle.FinalizeReport();

            var inspection = ReportArtifactInspector.Inspect(reportPath);
            Assert.True(inspection.IsCompliant, string.Join(", ", inspection.Violations));
            Assert.Contains("GET /public/v2/users", File.ReadAllText(reportPath), StringComparison.Ordinal);
            Assert.Contains("Assertions", File.ReadAllText(reportPath), StringComparison.Ordinal);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    private static void PublishScenario(
        ExtentReportLifecycle lifecycle,
        string testClassName,
        string scenarioName,
        AtfReportStatus status)
    {
        using var scenario = lifecycle.StartTest(
            testClassName,
            scenarioName,
            "category status contract",
            ["mixed-status"]);

        scenario.CompleteScenario(status);
    }
}
