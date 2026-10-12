using Atf.Core.Reporting;
using AventStack.ExtentReports;

namespace Atf.Core.Tests;

public sealed class ReportingComponentTests
{
    [Fact(DisplayName = "Report theme expands the central palette without leaking placeholders")]
    public void ReportThemeUsesCentralPalette()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(AtfReportColors.Dark, css, StringComparison.Ordinal);
        Assert.Contains(AtfReportColors.JsonSurface, css, StringComparison.Ordinal);
        Assert.Contains(AtfReportColors.SecondarySectionHeadingBackground, css, StringComparison.Ordinal);
        Assert.Contains("--atf-secondary-heading-text: var(--atf-accent)", css, StringComparison.Ordinal);
        Assert.Contains($"--atf-secondary-audit-text: {AtfReportColors.SecondaryAuditDescriptionText}", css, StringComparison.Ordinal);
        Assert.Contains("atf-tag-card .card-header", css, StringComparison.Ordinal);
        Assert.Contains(".atf-tag-card .card-header", css, StringComparison.Ordinal);
        Assert.Contains("background: var(--atf-panel-2) !important; color: var(--atf-secondary-audit-text) !important", css, StringComparison.Ordinal);
        Assert.Contains(".atf-tag-audit-heading p", css, StringComparison.Ordinal);
        Assert.Contains("color: var(--atf-muted) !important", css, StringComparison.Ordinal);
        Assert.Contains("color: var(--atf-secondary-audit-text) !important", css, StringComparison.Ordinal);
        Assert.Contains("--atf-dark: #101723", css, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Extent asset inliner embeds the offline resources")]
    public void ExtentAssetsAreEmbedded()
    {
        var html = new ExtentAssetInliner().Inline("""
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

    [Fact(DisplayName = "Extent evidence renderer keeps details collapsed and escapes values")]
    public void EvidenceRendererBuildsSafeCollapsedEnvelope()
    {
        var renderer = new ExtentEvidenceRenderer();

        var evidence = renderer.Build(
            "Response details",
            $"<p>{renderer.Html("<secret>")}</p>",
            kind: "response-details");

        Assert.Contains("<details class='step-detail response-details'>", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain(" open", evidence, StringComparison.Ordinal);
        Assert.Contains("&lt;secret&gt;", evidence, StringComparison.Ordinal);
        Assert.Equal("REQUEST JSON", renderer.JsonDisplayLabel("request.body"));
        Assert.Equal("RESPONSE JSON", renderer.JsonDisplayLabel("response.body"));
    }

    [Fact(DisplayName = "Report status aggregator gives failure precedence over skip and pass")]
    public void FailureHasHighestPrecedence()
    {
        var aggregator = new ReportStatusAggregator();

        Assert.Equal(Status.Pass, aggregator.Record("UsersApiTests", Status.Pass));
        Assert.Equal(Status.Skip, aggregator.Record("UsersApiTests", Status.Skip));
        Assert.Equal(Status.Fail, aggregator.Record("UsersApiTests", Status.Fail));
        Assert.Equal(Status.Fail, aggregator.Record("UsersApiTests", Status.Pass));
        Assert.True(aggregator.TryGet("UsersApiTests", out var status));
        Assert.Equal(Status.Fail, status);
    }

    [Fact(DisplayName = "Report status aggregation is isolated per logical test class")]
    public void StatusIsScopedByLogicalTest()
    {
        var aggregator = new ReportStatusAggregator();

        aggregator.Record("UsersApiTests", Status.Fail);
        aggregator.Record("PostsApiTests", Status.Pass);

        Assert.True(aggregator.TryGet("UsersApiTests", out var userStatus));
        Assert.True(aggregator.TryGet("PostsApiTests", out var postStatus));
        Assert.Equal(Status.Fail, userStatus);
        Assert.Equal(Status.Pass, postStatus);
    }

    [Fact(DisplayName = "Execution evidence classifies response details and keeps the compact trace")]
    public void ResponseEvidenceUsesResponseContract()
    {
        var evidence = ExecutionEvidenceComposer.Compose(
            "GET /public/v2/users",
            [
                new("method", "GET"),
                new("url", "https://example.test/users"),
                new("auth.profile", "anonymous"),
                new("response.status", "200"),
                new("response.content.type", "application/json"),
                new("response.duration.ms", "42"),
                new("exchange.duration.ms", "42")
            ]);

        Assert.Equal("Response details · 6 fields", evidence.Title);
        Assert.Equal("response-details", evidence.Kind);
        Assert.False(evidence.IsAssertion);
        Assert.Contains("ENDPOINT", evidence.BodyHtml);
        Assert.DoesNotContain("TOTAL DURATION", evidence.BodyHtml);
        Assert.Contains("RESPONSE STATUS", evidence.BodyHtml);
    }

    [Fact(DisplayName = "Execution evidence renders assertion details as one escaped table")]
    public void AssertionEvidenceUsesOneTable()
    {
        var evidence = ExecutionEvidenceComposer.Compose(
            "Validate: HTTP status",
            [
                new("expected", "200 OK"),
                new("actual", "<503> & retry")
            ]);

        Assert.Equal("Assertion details", evidence.Title);
        Assert.Equal(string.Empty, evidence.Kind);
        Assert.True(evidence.IsAssertion);
        Assert.Contains("helm-assertion-table", evidence.BodyHtml);
        Assert.Contains("&lt;503&gt; &amp; retry", evidence.BodyHtml);
        Assert.Equal(1, Count(evidence.BodyHtml, "<table"));
    }

    [Fact(DisplayName = "Execution evidence uses execution summary when no request or response metadata exists")]
    public void GenericEvidenceUsesExecutionSummary()
    {
        var evidence = ExecutionEvidenceComposer.Compose(
            "TEST BODY",
            [new("fixture.userId", "123")]);

        Assert.Equal("Execution summary · 1 fields", evidence.Title);
        Assert.Equal(string.Empty, evidence.Kind);
        Assert.Contains("FIXTURE USERID", evidence.BodyHtml);
    }

    private static int Count(string text, string value) =>
        text.Split(value, StringSplitOptions.None).Length - 1;
}
