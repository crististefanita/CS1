using Atf.Core.Reporting;
using Atf.Core.Reporting.Evidence;
using Atf.Reporting.Extent.Evidence;
using AventStack.ExtentReports;

namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingComponentTests
{

    [Fact(DisplayName = "Warning assertion rows use the warning emoji instead of the failure emoji")]
    public void WarningAssertionRowsUseWarningEmoji()
    {
        var html = AssertionEvidenceFormatter.BuildTableHtml([
            new AtfAssertion("HTTP status", "201 Created", "200 OK", false, AtfReportStatus.Warning)
        ]);

        Assert.Contains("aria-label='Warning'", html, StringComparison.Ordinal);
        Assert.Contains("title='Warning'>⚠️</span>", html, StringComparison.Ordinal);
        Assert.Contains("class='assertion-warning'", html, StringComparison.Ordinal);
        Assert.Contains("data-atf-assertion-status='warning'", html, StringComparison.Ordinal);
        Assert.DoesNotContain("class='assertion-fail'", html, StringComparison.Ordinal);
        Assert.DoesNotContain("title='Fail'>❌</span>", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Assertion evidence uses typed row classes for pass, warning, skip and fail")]
    public void AssertionEvidenceUsesTypedRowClasses()
    {
        var html = AssertionEvidenceFormatter.BuildTableHtml([
            new AtfAssertion("pass", "expected", "actual", true, AtfReportStatus.Pass),
            new AtfAssertion("warning", "expected", "actual", false, AtfReportStatus.Warning),
            new AtfAssertion("skip", "expected", "actual", false, AtfReportStatus.Skip),
            new AtfAssertion("fail", "expected", "actual", false, AtfReportStatus.Fail)
        ]);

        Assert.Contains("class='assertion-pass' data-atf-assertion-status='pass'", html, StringComparison.Ordinal);
        Assert.Contains("class='assertion-warning' data-atf-assertion-status='warning'", html, StringComparison.Ordinal);
        Assert.Contains("class='assertion-skip' data-atf-assertion-status='skip'", html, StringComparison.Ordinal);
        Assert.Contains("class='assertion-fail' data-atf-assertion-status='fail'", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Theme and client behaviour compose all semantic fragments")]
    public void ReportingComposersResolveTheirFragmentBoundaries()
    {
        var css = AtfReportTheme.BuildCss();
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains(".category-view .badge.log", css, StringComparison.Ordinal);
        Assert.Contains(".helm-trace-table", css, StringComparison.Ordinal);
        Assert.Contains("function buildTagAudit()", script, StringComparison.Ordinal);
        Assert.Contains("function configureFailureView()", script, StringComparison.Ordinal);
        Assert.Contains("function configureExecutionHierarchy()", script, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_", css, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Extent asset inliner embeds the offline resources")]
    public void ExtentAssetsAreEmbedded()
    {
        var html = ExtentAssetInliner.Inline("""
            <link href="extent/spark-style.css" rel="stylesheet" />
            <link href="extent/font-awesome.min.css" rel="stylesheet">
            <script src="extent/jsontree.js"></script>
            <script src="extent/spark-script.js"></script>
            <img src="extent/logo.png">
            """);

        Assert.DoesNotContain("extent/spark-style.css", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("extent/font-awesome.min.css", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("extent/jsontree.js", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("extent/spark-script.js", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("extent/logo.png", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data:", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Report composition removes the native Log events card structurally")]
    public void ReportCompositionRemovesNativeLogEventsCard()
    {
        const string html = """
            <div class="row">
              <div class="col-md-3"><div class="card"><div class="card-header"><h6 class="card-title">Tests</h6></div><div class="card-body"><canvas /></div></div></div>
              <div class="col-md-3"><div class="card"><div class="card-header"><h6 class="card-title">Log events</h6></div><div class="card-body"><canvas id="events-analysis" /></div><div class="card-footer"><div>old counter</div></div></div></div>
              <div class="col-md-3"><div class="card"><h6 class="card-title">Keep this</h6></div></div>
            </div>
            """;

        var cleaned = ExtentNativeOverviewPruner.RemoveNativeLogEventsCard(html);

        Assert.DoesNotContain("Log events", cleaned, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("events-analysis", cleaned, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Keep this", cleaned, StringComparison.Ordinal);
        Assert.Contains("Tests", cleaned, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Report composition removes the orphaned native events chart script")]
    public void ReportCompositionRemovesNativeEventsChartScript()
    {
        const string html = "<body>before</body>" +
            "/* -- [ events chart ] -- */ (function drawEventsChart() { var stale = true; })();";

        var cleaned = ExtentNativeOverviewPruner.RemoveNativeEventsChart(html);

        Assert.DoesNotContain("drawEventsChart", cleaned, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("events chart", cleaned, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("before", cleaned, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Embedded report behaviour exposes dependency-free view navigation")]
    public void ClientBehaviourProvidesViewNavigation()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("function installNavigation()", script, StringComparison.Ordinal);
        Assert.Contains("window.toggleView = function (view)", script, StringComparison.Ordinal);
        Assert.Contains(".main-content > .view", script, StringComparison.Ordinal);
        Assert.Contains("candidate.classList.toggle('d-none'", script, StringComparison.Ordinal);
        Assert.Contains("window.currentView = view", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Failure behaviour composes evidence records before the DOM view projection")]
    public void FailureBehaviourKeepsEvidenceAndViewResponsibilitiesSeparated()
    {
        var script = ExtentReportClientBehaviour.Build();

        var evidenceBoundary = script.IndexOf("function failureEvidenceRecords(card)", StringComparison.Ordinal);
        var viewBoundary = script.IndexOf("function createFailureViewItem(record)", StringComparison.Ordinal);

        Assert.True(evidenceBoundary >= 0, "The failure evidence fragment must be composed into the client contract.");
        Assert.True(viewBoundary > evidenceBoundary, "Failure records must be prepared before the DOM view is rendered.");
        Assert.Equal(1, CountOccurrences(script, "function failureEvidenceRecords(card)"));
        Assert.Equal(1, CountOccurrences(script, "function createFailureViewItem(record)"));
        Assert.Contains("scenarioCard.dataset.atfFailureIds", script, StringComparison.Ordinal);
        Assert.Contains("function evidenceKey(value, fallback)", script, StringComparison.Ordinal);
        Assert.Contains("const evidenceId = [phaseId, requestId, validationCardId, assertionId].join('::')", script, StringComparison.Ordinal);
        Assert.Contains("evidenceId: failure.evidence.evidenceId", script, StringComparison.Ordinal);
        Assert.Contains("data-atf-evidence-id", script, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += token.Length;
        }

        return count;
    }

}
