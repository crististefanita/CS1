using Atf.Core.Reporting;

namespace Atf.Core.Tests;

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

            var inspection = new ReportArtifactInspector().Inspect(report);

            Assert.True(inspection.IsCompliant);
            Assert.Empty(inspection.Violations);
            Assert.True(inspection.Satisfies("collapsed-evidence"));
            Assert.True(inspection.Satisfies("inner-phase-rail-removed"));
        }
        finally
        {
            root.Delete(recursive: true);
        }
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

            var inspection = new ReportArtifactInspector().Inspect(report);

            Assert.False(inspection.IsCompliant);
            Assert.Contains("self-contained", inspection.Violations);
            Assert.Contains("extent-folder-removed", inspection.Violations);
            Assert.Contains("collapsed-evidence", inspection.Violations);
            Assert.Contains("assertion-table", inspection.Violations);
        }
        finally
        {
            root.Delete(recursive: true);
        }
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

            var inspection = new ReportArtifactInspector().Inspect(report);

            Assert.False(inspection.Satisfies("self-contained"));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private const string CompliantHtml = """
    <html>
      <head>
        <style id="atf-custom-theme">
          :root { --atf-dark: #101723; --atf-bg: var(--atf-dark); }
          .timestamp-col { display: none !important; }
          .card[data-atf-phase] > .accordion { border-left: 0 !important; }
        </style>
      </head>
      <body>
        <details class='step-detail'><summary>REQUEST JSON</summary>
          <div class='helm-scroll-block-body'><pre>{}</pre></div>
        </details>
        <table class='helm-validation-table'><tbody></tbody></table>
        <div>TECHNICAL TRACE</div>
      </body>
    </html>
    """;
}
