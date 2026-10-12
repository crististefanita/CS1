using Atf.Core.Reporting;
using AventStack.ExtentReports;

namespace Atf.Core.Tests.Reporting;

public sealed class ReportingComponentTests
{
    [Fact(DisplayName = "Report theme expands the central palette without leaking placeholders")]
    public void ReportThemeUsesCentralPalette()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(AtfReportColors.Dark, css, StringComparison.Ordinal);
        Assert.Contains(AtfReportColors.JsonSurface, css, StringComparison.Ordinal);
        Assert.Contains($"--atf-text: {AtfReportColors.Text}", css, StringComparison.Ordinal);
        Assert.Contains(AtfReportColors.SecondarySectionHeadingBackground, css, StringComparison.Ordinal);
        Assert.Contains("--atf-secondary-heading-text: var(--atf-accent)", css, StringComparison.Ordinal);
        Assert.Contains($"--atf-secondary-audit-text: {AtfReportColors.SecondaryAuditDescriptionText}", css, StringComparison.Ordinal);
        Assert.Contains("atf-tag-card .card-header", css, StringComparison.Ordinal);
        Assert.Contains(".atf-tag-card .card-header", css, StringComparison.Ordinal);
        Assert.Contains("background: var(--atf-panel-2) !important; color: var(--atf-secondary-audit-text) !important", css, StringComparison.Ordinal);
        Assert.Contains("body.dark .atf-tag-table, body.dark .atf-tag-table tbody, body.dark .atf-tag-table tr, body.dark .atf-tag-table td { color: var(--atf-text) !important; }", css, StringComparison.Ordinal);
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

    [Fact(DisplayName = "Extent composition activates the native dark theme")]
    public void ExtentDocumentUsesDarkTheme()
    {
        const string standardHtml = "<body class=\"spa standard\"><main /></body>";
        const string paddedStandardHtml = "<body class=\"spa  standard\"><main /></body>";
        const string singleQuotedStandardHtml = "<body class='spa standard'><main /></body>";

        Assert.Contains("class=\"spa dark\"", ExtentReportDocumentComposer.EnsureDarkExtentTheme(standardHtml), StringComparison.Ordinal);
        Assert.Contains("class=\"spa  dark\"", ExtentReportDocumentComposer.EnsureDarkExtentTheme(paddedStandardHtml), StringComparison.Ordinal);
        Assert.Contains("class='spa dark'", ExtentReportDocumentComposer.EnsureDarkExtentTheme(singleQuotedStandardHtml), StringComparison.Ordinal);
        Assert.Equal("<body class=\"spa dark\"><main /></body>", ExtentReportDocumentComposer.EnsureDarkExtentTheme("<body class=\"spa dark\"><main /></body>"));
    }

    [Fact(DisplayName = "Report composition replaces stale ATF-owned CSS and behaviour blocks")]
    public void ReportCompositionRemovesStaleAtfBlocks()
    {
        const string html = "<head><style id=\"atf-custom-theme\">old-css</style></head><body><script id='atf-custom-behaviour'>old-js</script><main /></body>";

        var cleaned = ExtentReportDocumentComposer.RemoveAtfOwnedBlocks(html);

        Assert.DoesNotContain("old-css", cleaned, StringComparison.Ordinal);
        Assert.DoesNotContain("old-js", cleaned, StringComparison.Ordinal);
        Assert.DoesNotContain("atf-custom-theme", cleaned, StringComparison.Ordinal);
        Assert.DoesNotContain("atf-custom-behaviour", cleaned, StringComparison.Ordinal);
        Assert.Contains("<main />", cleaned, StringComparison.Ordinal);
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

    [Fact(DisplayName = "Client behaviour embeds configured native navigation order")]
    public void ClientBehaviourEmbedsConfiguredNavigationOrder()
    {
        var script = ExtentReportClientBehaviour.Build(
            new ReportingSettings
            {
                TestClassOrder = ["UsersApiTests", "PostsApiTests"],
                TestScenarioOrder = ["USERS API — test only"],
                TagOrder = ["api", "smoke"]
            });

        Assert.Contains(
            $"const classOrder = {System.Text.Json.JsonSerializer.Serialize(new[] { "UsersApiTests", "PostsApiTests" })}",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            $"const scenarioOrder = {System.Text.Json.JsonSerializer.Serialize(new[] { "USERS API — test only" })}",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            $"const tagOrder = {System.Text.Json.JsonSerializer.Serialize(new[] { "api", "smoke" })}",
            script,
            StringComparison.Ordinal);
        Assert.Contains("function directScenarioCards(item)", script, StringComparison.Ordinal);
        Assert.Contains("const cards = directScenarioCards(item)", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Client behaviour keeps lifecycle phase shells compact and collapsed")]
    public void ClientBehaviourUsesCollapsedPhaseShellContract()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("body.classList.remove('show')", script, StringComparison.Ordinal);
        Assert.Contains("body.style.setProperty('display', 'none', 'important')", script, StringComparison.Ordinal);
        Assert.Contains("header.setAttribute('aria-expanded', 'false')", script, StringComparison.Ordinal);
        Assert.Contains("function removeCompositeScenarioSummary()", script, StringComparison.Ordinal);
        Assert.Contains("const summary = directChild(card, 'collapse')", script, StringComparison.Ordinal);
        Assert.Contains("if (summary) summary.remove()", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Client behaviour reconciles native overview counters from API scenario cards")]
    public void ClientBehaviourCountsScenariosInsteadOfClassRootsOrAssemblyTests()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("function reportScenarioData()", script, StringComparison.Ordinal);
        Assert.Contains("const total = data.scenarioCards.length", script, StringComparison.Ordinal);
        Assert.Contains("setText(summaryValue('Tests Passed'), totals.passed)", script, StringComparison.Ordinal);
        Assert.Contains("setText(summaryValue('Tests Failed'), totals.failed)", script, StringComparison.Ordinal);
        Assert.Contains("statusGroup.parentCount = total", script, StringComparison.Ordinal);
        Assert.Contains("chart?.canvas?.id !== 'parent-analysis'", script, StringComparison.Ordinal);
        Assert.Contains("const nativeTagTable = dashboard.querySelector('.category-container table')", script, StringComparison.Ordinal);
        Assert.Contains("const tagTotal = totals.passed + totals.failed + totals.skipped + totals.other", script, StringComparison.Ordinal);
        Assert.Contains("cells[5].textContent = Math.round(totals.passed / Math.max(tagTotal, 1) * 100) + '%'", script, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_TEST_TOTAL__", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Report client behaviour keeps API nodes collapsed until the user opens them")]
    public void ClientBehaviourKeepsNodesCollapsedByDefault()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("body.style.setProperty('display', 'none', 'important')", script, StringComparison.Ordinal);
        Assert.Contains("body.classList.remove('show')", script, StringComparison.Ordinal);
        Assert.Contains("details.step-detail", script, StringComparison.Ordinal);
        Assert.DoesNotContain("<details class='step-detail' open", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Client behaviour preserves phase request-validation siblings")]
    public void ClientBehaviourMarksExecutionRoles()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("function configureExecutionHierarchy()", script, StringComparison.Ordinal);
        Assert.Contains("data-atf-node-role', 'phase", script, StringComparison.Ordinal);
        Assert.Contains("stepCard.setAttribute('data-atf-node-role', role)", script, StringComparison.Ordinal);
        Assert.Contains("const role = /^validate\\s*:/i.test(titleText) ? 'validation' : 'request'", script, StringComparison.Ordinal);
        Assert.Contains("/^validate\\s*:/i.test(titleText)", script, StringComparison.Ordinal);
        Assert.Contains("data-atf-parent-node', 'phase", script, StringComparison.Ordinal);
        Assert.DoesNotContain("data-atf-node-group', 'validations", script, StringComparison.Ordinal);
        Assert.DoesNotContain("data-atf-parent-node', 'request", script, StringComparison.Ordinal);
        Assert.Contains("configureExecutionHierarchy();", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Client behaviour owns nested card clicks in capture phase")]
    public void ClientBehaviourDelegatesIndependentCardToggles()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("function installCardDelegation()", script, StringComparison.Ordinal);
        Assert.Contains("document.addEventListener('click'", script, StringComparison.Ordinal);
        Assert.Contains("event.stopImmediatePropagation()", script, StringComparison.Ordinal);
        Assert.Contains("toggleCard(header.parentElement, header, event)", script, StringComparison.Ordinal);
        Assert.Contains("installCardDelegation();", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Report theme keeps JSON readable without horizontal page overflow")]
    public void ReportThemeUsesVerticalJsonScrollAndWrappedValues()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains("--atf-audit-scroll-height: 340px", css, StringComparison.Ordinal);
        Assert.Contains(".atf-tag-scroll { height: var(--atf-audit-scroll-height) !important; max-height: var(--atf-audit-scroll-height) !important;", css, StringComparison.Ordinal);
        Assert.Contains(".helm-scroll-block-body { max-height: var(--atf-audit-scroll-height) !important;", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".helm-scroll-block-body { height:", css, StringComparison.Ordinal);
        Assert.Contains("overflow-y: auto !important", css, StringComparison.Ordinal);
        Assert.Contains(".helm-scroll-block-body pre { box-sizing: border-box !important; min-height: 0 !important;", css, StringComparison.Ordinal);
        Assert.Contains("white-space: pre-wrap !important", css, StringComparison.Ordinal);
        Assert.Contains("word-break: break-word !important", css, StringComparison.Ordinal);
        Assert.Contains(".test-wrapper, .test-content, .test-content-detail { overflow-x: hidden !important; }", css, StringComparison.Ordinal);
        Assert.Contains(".helm-trace-table thead th:first-child", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Report theme visually separates request and validation siblings")]
    public void ReportThemeUsesPhaseExecutionFrames()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains("card[data-atf-node-role=\"request\"]", css, StringComparison.Ordinal);
        Assert.Contains("card[data-atf-node-role=\"validation\"]", css, StringComparison.Ordinal);
        Assert.DoesNotContain("content: \"CONTRACT VALIDATION\"", css, StringComparison.Ordinal);
        Assert.Contains("contract validations are sibling steps", css, StringComparison.Ordinal);
        Assert.Contains("data-atf-parent-phase", ExtentReportClientBehaviour.Build(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Report node interaction binds directly to headers and keeps nodes collapsed")]
    public void ClientBehaviourBindsDirectCardToggles()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("bindCardToggle(card, header);", script, StringComparison.Ordinal);
        Assert.Contains("window.addEventListener('click', function (event)", script, StringComparison.Ordinal);
        Assert.Contains("window.__atfCaptureToggleInstalled", script, StringComparison.Ordinal);
        Assert.Contains("targetHeader?.__atfToggleHandler", script, StringComparison.Ordinal);
        Assert.Contains("body.style.setProperty('display', expanded ? 'block' : 'none', 'important');", script, StringComparison.Ordinal);
        Assert.DoesNotContain("document.addEventListener('click', toggleCard, true)", script, StringComparison.Ordinal);
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

        Assert.Equal(AtfReportStatus.Pass, aggregator.Record("UsersApiTests", AtfReportStatus.Pass));
        Assert.Equal(AtfReportStatus.Skip, aggregator.Record("UsersApiTests", AtfReportStatus.Skip));
        Assert.Equal(AtfReportStatus.Fail, aggregator.Record("UsersApiTests", AtfReportStatus.Fail));
        Assert.Equal(AtfReportStatus.Fail, aggregator.Record("UsersApiTests", AtfReportStatus.Pass));
        Assert.True(aggregator.TryGet("UsersApiTests", out var status));
        Assert.Equal(AtfReportStatus.Fail, status);
    }

    [Fact(DisplayName = "Report status aggregation is isolated per logical test class")]
    public void StatusIsScopedByLogicalTest()
    {
        var aggregator = new ReportStatusAggregator();

        aggregator.Record("UsersApiTests", AtfReportStatus.Fail);
        aggregator.Record("PostsApiTests", AtfReportStatus.Pass);

        Assert.True(aggregator.TryGet("UsersApiTests", out var userStatus));
        Assert.True(aggregator.TryGet("PostsApiTests", out var postStatus));
        Assert.Equal(AtfReportStatus.Fail, userStatus);
        Assert.Equal(AtfReportStatus.Pass, postStatus);
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

    [Fact(DisplayName = "Technical trace keeps route policy metadata out of the compact response contract")]
    public void TechnicalTraceOmitsRoutePolicyMetadata()
    {
        var details = EvidenceTableFormatter.CompactTechnicalTraceDetails(
        [
            new("method", "GET"),
            new("url", "https://example.test/users/1"),
            new("route.path.template", "/users/{id}"),
            new("route.auth.type", "none"),
            new("route.auth.requirement", "optional"),
            new("route.auth.profile", "anonymous"),
            new("auth.profile", "anonymous"),
            new("response.status", "200"),
            new("response.content.type", "application/json"),
            new("response.duration.ms", "42"),
            new("attempt", "1")
        ]);

        Assert.Equal(8, details.Count);
        Assert.DoesNotContain(details, detail => detail.Key.StartsWith("route.auth.", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            ["method", "url", "route.path.template", "auth.profile", "response.status", "response.content.type", "response.duration.ms", "attempt"],
            details.Select(detail => detail.Key).ToArray());
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

    [Fact(DisplayName = "Execution evidence classifies context-only fields as generic metadata")]
    public void GenericEvidenceIsIdentifiedAsContextOnly()
    {
        var evidence = ExecutionEvidenceComposer.Compose(
            "TEST BODY",
            [new("fixture.userId", "123")]);

        Assert.Equal("Execution summary · 1 fields", evidence.Title);
        Assert.Equal(string.Empty, evidence.Kind);
        Assert.True(evidence.IsGenericSummary);
        Assert.Contains("FIXTURE USERID", evidence.BodyHtml);
    }

    private static int Count(string text, string value) =>
        text.Split(value, StringSplitOptions.None).Length - 1;
}
