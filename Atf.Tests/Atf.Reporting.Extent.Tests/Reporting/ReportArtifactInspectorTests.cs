namespace Atf.Reporting.Extent.Tests.Reporting;

public sealed class ReportArtifactInspectorTests
{
    [Fact(DisplayName = "Report artifact inspector accepts the complete API report contract")]
    public void AcceptsCompliantArtifact()
    {
        var root = Directory.CreateTempSubdirectory("atf-report-contract-");
        try
        {
            var report = Path.Combine(root.FullName, "report.html");
            File.WriteAllText(report, CompliantHtml);

            var inspection = ReportArtifactInspector.Inspect(report);

            Assert.True(inspection.IsCompliant);
            Assert.Empty(inspection.Violations);
            Assert.True(inspection.Satisfies("collapsed-evidence"));
            Assert.True(inspection.Satisfies("inner-phase-rail-removed"));
            Assert.True(inspection.Satisfies("composite-summary-cleanup"));
            Assert.True(inspection.Satisfies("phase-shells-collapsed"));
            Assert.True(inspection.Satisfies("response-owned-execution-hierarchy"));
            Assert.True(inspection.Satisfies("response-duration-ms"));
            Assert.True(inspection.Satisfies("no-generic-framework-tag"));
            Assert.True(inspection.Satisfies("accessible-evidence-titles"));
            Assert.True(inspection.Satisfies("semantic-execution-roles"));
            Assert.True(inspection.Satisfies("secondary-overview-collapsed"));
        }
        finally { root.Delete(recursive: true); }
    }

    [Fact(DisplayName = "Report artifact inspector identifies external assets and missing evidence contracts")]
    public void ReportsArtifactViolations()
    {
        var root = Directory.CreateTempSubdirectory("atf-report-contract-");
        try
        {
            var report = Path.Combine(root.FullName, "report.html");
            Directory.CreateDirectory(Path.Combine(root.FullName, "extent"));
            File.WriteAllText(report, "<link href=\"extent/spark-style.css\"><details class='step-detail' open></details>");

            var inspection = ReportArtifactInspector.Inspect(report);

            Assert.False(inspection.IsCompliant);
            Assert.Contains("self-contained", inspection.Violations);
            Assert.Contains("extent-folder-removed", inspection.Violations);
            Assert.Contains("collapsed-evidence", inspection.Violations);
            Assert.Contains("assertion-table", inspection.Violations);
        }
        finally { root.Delete(recursive: true); }
    }

    [Theory(DisplayName = "Report artifact inspector rejects every common external Extent asset syntax")]
    [InlineData("<link href='extent/spark.css'>")]
    [InlineData("<script src='extent/spark.js'></script>")]
    [InlineData(".icon { background: url('extent/icon.svg'); }")]
    public void RejectsExternalAssetSyntax(string html)
    {
        var root = Directory.CreateTempSubdirectory("atf-report-contract-");
        try
        {
            var report = Path.Combine(root.FullName, "report.html");
            File.WriteAllText(report, html);
            Assert.False(ReportArtifactInspector.Inspect(report).Satisfies("self-contained"));
        }
        finally { root.Delete(recursive: true); }
    }

    [Fact(DisplayName = "Report artifact inspector rejects an open step detail regardless of quote style")]
    public void RejectsOpenDetailsWithDoubleQuotes()
    {
        var root = Directory.CreateTempSubdirectory("atf-report-contract-");
        try
        {
            var report = Path.Combine(root.FullName, "report.html");
            File.WriteAllText(report, "<details class=\"step-detail\" open=\"open\"><summary>request</summary></details>");
            Assert.False(ReportArtifactInspector.Inspect(report).Satisfies("collapsed-evidence"));
        }
        finally { root.Delete(recursive: true); }
    }

    [Fact(DisplayName = "Report artifact inspector requires the canonical assertion table headers")]
    public void RequiresAssertionTableHeaders()
    {
        var root = Directory.CreateTempSubdirectory("atf-report-contract-");
        try
        {
            var report = Path.Combine(root.FullName, "report.html");
            File.WriteAllText(report, "<table class='helm-validation-table'><thead><tr><th>ASSERTION</th><th>EXPECTED</th><th>ACTUAL</th></tr></thead></table>");
            var inspection = ReportArtifactInspector.Inspect(report);
            Assert.True(inspection.Satisfies("assertion-table"));
            Assert.False(inspection.Satisfies("assertion-table-headers"));
        }
        finally { root.Delete(recursive: true); }
    }

    [Fact(DisplayName = "Report artifact inspector requires a vertical JSON scroll contract and page overflow guard")]
    public void RequiresJsonAndPageOverflowGuards()
    {
        var root = Directory.CreateTempSubdirectory("atf-report-contract-");
        try
        {
            var report = Path.Combine(root.FullName, "report.html");
            File.WriteAllText(report, "<style>.helm-scroll-block-body { overflow-x: auto; }</style><div class='helm-scroll-block-body'></div>");
            var inspection = ReportArtifactInspector.Inspect(report);
            Assert.False(inspection.Satisfies("scrollable-json"));
            Assert.False(inspection.Satisfies("page-horizontal-overflow-guard"));
        }
        finally { root.Delete(recursive: true); }
    }

    [Fact(DisplayName = "Report artifact inspector rejects the generic framework tag")]
    public void RejectsGenericFrameworkTag()
    {
        var root = Directory.CreateTempSubdirectory("atf-report-contract-");
        try
        {
            var report = Path.Combine(root.FullName, "report.html");
            File.WriteAllText(report, "<div tag=\"api lifecycle framework\"></div><a class=\"dropdown-item\">framework</a>");
            Assert.False(ReportArtifactInspector.Inspect(report).Satisfies("no-generic-framework-tag"));
        }
        finally { root.Delete(recursive: true); }
    }

    private const string CompliantHtml = """
    <html>
      <head>
        <style id="atf-custom-theme">
          :root { --atf-dark: #101723; --atf-bg: var(--atf-dark); }
          .timestamp-col { display: none !important; }
          .card[data-atf-phase] > .accordion { border-left: 0 !important; }
          .card[data-atf-node-role="request"] {}
          .card[data-atf-node-role="validation"] {}
          .helm-scroll-block-body { overflow-y: auto; overflow-x: hidden; }
          .test-wrapper, .test-content, .test-content-detail { overflow-x: hidden; }
          [data-atf-parent-phase] {}
          .atf-secondary-overview { }
        </style>
      </head>
      <body>
        <script id="atf-custom-behaviour">
          function configureExecutionHierarchy() {}
           phaseCard.setAttribute('data-atf-node-role', 'phase');
           stepCard.setAttribute('data-atf-node-role', 'request');
           stepCard.setAttribute('data-atf-parent-node', 'phase');
           validationCard.setAttribute('data-atf-node-role', 'validation');
           validationCard.setAttribute('data-atf-parent-node', 'phase');
          function removeCompositeScenarioSummary() {}
          body.classList.remove('show');
          header.setAttribute('aria-expanded', 'false');
        </script>
        <details class='atf-secondary-overview'><summary>metadata</summary></details>
        <details class='atf-secondary-overview'><summary>tag audit</summary></details>
        <details class='step-detail'><summary>REQUEST JSON</summary>
          <div class='helm-scroll-block-body'><pre>{}</pre></div>
        </details>
        <span class='se-title' title='Response details' aria-label='Response details'>Response details</span>
        <div>RESPONSE DURATION (ms)</div>
        <div data-atf-node-role='phase' data-atf-parent-node='phase'></div>
        <div data-atf-node-role='request' data-atf-parent-node='phase'></div>
        <div data-atf-node-role='validation' data-atf-parent-node='phase'></div>
        <table class='helm-validation-table'><thead><tr><th>ASSERTION</th><th>EXPECTED</th><th>ACTUAL</th><th>RESULT</th></tr></thead><tbody><tr><td>status</td><td>200</td><td>200</td><td>✅</td></tr></tbody></table>
        <div>TECHNICAL TRACE</div>
      </body>
    </html>
    """;
}
