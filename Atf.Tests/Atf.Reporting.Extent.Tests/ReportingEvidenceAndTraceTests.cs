namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingEvidenceAndTraceTests
{
    [Fact(DisplayName = "Evidence tables keep their result column inside the viewport")]
    public void EvidenceTablesUseBorderBoxSizing()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(".helm-evidence-table { box-sizing: border-box !important;", css, StringComparison.Ordinal);
        Assert.Contains(".helm-evidence-table th, .helm-evidence-table td { box-sizing: border-box !important;", css, StringComparison.Ordinal);
        Assert.Contains(".helm-validation-table th:nth-child(4), .helm-validation-table td:nth-child(4) { width: 12% !important;", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Client behaviour marks request and validation as phase siblings")]
    public void ClientBehaviourMarksExecutionRoles()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("function configureExecutionHierarchy()", script, StringComparison.Ordinal);
        Assert.Contains("data-atf-node-role', 'phase", script, StringComparison.Ordinal);
        Assert.Contains("/^validate\\s*:/i.test(validationText)", script, StringComparison.Ordinal);
        Assert.Contains("data-atf-parent-node', 'phase", script, StringComparison.Ordinal);
        Assert.DoesNotContain("data-atf-node-group', 'validations", script, StringComparison.Ordinal);
        Assert.Contains("stepCard.setAttribute('data-atf-parent-node', 'phase')", script, StringComparison.Ordinal);
        Assert.DoesNotContain("const requestAccordion = directChild(stepCard, 'accordion')", script, StringComparison.Ordinal);
        Assert.Contains("validationCard.setAttribute('data-atf-node-role', 'validation')", script, StringComparison.Ordinal);
        Assert.Contains("validationCard.setAttribute('data-atf-parent-node', 'phase')", script, StringComparison.Ordinal);
        Assert.DoesNotContain("data-atf-parent-node', 'request'", script, StringComparison.Ordinal);
        Assert.Contains("configureExecutionHierarchy();", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Execution metadata counts only outcomes emitted by the current run")]
    public void ClientBehaviourUsesRunOutcomesForTestTotal()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("const totalTests = totalTestsPassed + totalTestsFailed + totalTestsSkipped + scenarioStatusTotals.other", script, StringComparison.Ordinal);
        Assert.Contains("ATF tests in this run", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Reading rule:", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Authority: native Extent cards", script, StringComparison.Ordinal);
        Assert.DoesNotContain("discoveredTestCases", script, StringComparison.Ordinal);
        Assert.Contains("Total tests skipped", script, StringComparison.Ordinal);
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
        Assert.DoesNotContain("data-atf-node-role=\"request\"] > .collapse > .card-body > .accordion", css, StringComparison.Ordinal);
        Assert.Contains("data-atf-parent-phase", ExtentReportClientBehaviour.Build(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Report node interaction binds directly to headers and keeps nodes collapsed")]
    public void ClientBehaviourBindsDirectCardToggles()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("bindCardToggle(card, header);", script, StringComparison.Ordinal);
        Assert.Contains("window.addEventListener('click', function (event)", script, StringComparison.Ordinal);
        Assert.Contains("window.__atfCaptureToggleInstalled", script, StringComparison.Ordinal);
        Assert.Contains("header.__atfToggleHandler = activate", script, StringComparison.Ordinal);
        Assert.Contains("toggleCard(targetHeader.parentElement, targetHeader, event)", script, StringComparison.Ordinal);
        Assert.Contains("body.style.setProperty('display', expanded ? 'block' : 'none', 'important');", script, StringComparison.Ordinal);
        Assert.DoesNotContain("document.addEventListener('click', toggleCard, true)", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Navigation controller owns failure tab ordering")]
    public void NavigationControllerOwnsFailureTabOrdering()
    {
        var navigation = ExtentReportClientNavigationController.Build();
        var failure = ExtentReportClientFailureFragments.Build();

        Assert.Contains("function reconcileFailureNavigationOrder()", navigation, StringComparison.Ordinal);
        Assert.Contains("menu.insertBefore(failureItem, overviewItem)", navigation, StringComparison.Ordinal);
        Assert.DoesNotContain("function reconcileFailureNavigationOrder()", failure, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Failure evidence separates record factory from index projection")]
    public void FailureEvidenceSeparatesFactoryAndProjector()
    {
        var evidence = ExtentReportClientFailureEvidenceFragments.Build();
        var projection = ExtentReportClientFailureProjectionFragments.Build();
        var failureView = ExtentReportClientFailureFragments.Build();

        Assert.DoesNotContain("function createFailureRecord(", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("function projectFailureRecords()", evidence, StringComparison.Ordinal);
        Assert.Contains("function createFailureRecord(", projection, StringComparison.Ordinal);
        Assert.Contains("function collectFailureCandidates()", projection, StringComparison.Ordinal);
        Assert.Contains("function projectFailureRecords(candidates)", projection, StringComparison.Ordinal);
        Assert.Contains("return projectFailureRecords(collectFailureCandidates())", projection, StringComparison.Ordinal);
        Assert.Contains("failureScenarioRecords()", failureView, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Failure view resolves evidence before publishing evidence attributes")]
    public void FailureViewResolvesEvidenceBeforeUse()
    {
        var script = ExtentReportClientFailureFragments.Build();
        var evidenceDeclaration = script.IndexOf("const evidence = failureEvidence(card);", StringComparison.Ordinal);
        var evidenceAttribute = script.IndexOf("row.dataset.atfEvidenceId = evidence.evidenceId", StringComparison.Ordinal);

        Assert.True(evidenceDeclaration >= 0, "Failure view must resolve evidence before rendering metadata.");
        Assert.True(evidenceAttribute > evidenceDeclaration, "Evidence attributes must be assigned after evidence resolution.");
    }

    [Fact(DisplayName = "Extent evidence renderer keeps details collapsed and escapes values")]
    public void EvidenceRendererBuildsSafeCollapsedEnvelope()
    {
        var evidence = ExtentEvidenceRenderer.Build(
            "Response details",
            $"<p>{ExtentEvidenceRenderer.Html("<secret>")}</p>",
            kind: "response-details");

        Assert.Contains("<details class='step-detail response-details'>", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain(" open", evidence, StringComparison.Ordinal);
        Assert.Contains("&lt;secret&gt;", evidence, StringComparison.Ordinal);
        Assert.Equal("REQUEST JSON", ExtentEvidenceRenderer.JsonDisplayLabel("request.body"));
        Assert.Equal("RESPONSE JSON", ExtentEvidenceRenderer.JsonDisplayLabel("response.body"));
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

    [Fact(DisplayName = "Technical trace duplicate policy preserves all diagnostic occurrences when requested")]
    public void TechnicalTraceCanPreserveDuplicateOccurrences()
    {
        var details = EvidenceTableFormatter.CompactTechnicalTraceDetails(
        [
            new("method", "GET"),
            new("url", "https://example.test/users?page=1"),
            new("url", "https://example.test/users?page=2"),
            new("route.path.template", "/users"),
            new("auth.profile", "anonymous"),
            new("response.status", "429"),
            new("response.status", "200"),
            new("response.content.type", "application/json"),
            new("response.duration.ms", "42"),
            new("attempt", "1"),
            new("attempt", "2")
        ], DuplicateTechnicalTraceKeyPolicy.KeepAll);

        Assert.Equal(
            ["https://example.test/users?page=1", "https://example.test/users?page=2"],
            details.Where(detail => detail.Key.Equals("url", StringComparison.OrdinalIgnoreCase))
                .Select(detail => detail.Value)
                .ToArray());
        Assert.Equal(
            ["429", "200"],
            details.Where(detail => detail.Key.Equals("response.status", StringComparison.OrdinalIgnoreCase))
                .Select(detail => detail.Value)
                .ToArray());
        Assert.Equal(
            ["1", "2"],
            details.Where(detail => detail.Key.Equals("attempt", StringComparison.OrdinalIgnoreCase))
                .Select(detail => detail.Value)
                .ToArray());
    }

    [Fact(DisplayName = "Technical trace keeps final values by default for the compact response contract")]
    public void TechnicalTraceKeepsFinalValuesByDefault()
    {
        var details = EvidenceTableFormatter.CompactTechnicalTraceDetails(
        [
            new("method", "GET"),
            new("url", "https://example.test/users?page=1"),
            new("url", "https://example.test/users?page=2"),
            new("route.path.template", "/users"),
            new("auth.profile", "anonymous"),
            new("response.status", "429"),
            new("response.status", "200"),
            new("response.content.type", "application/json"),
            new("response.duration.ms", "42"),
            new("attempt", "1"),
            new("attempt", "2")
        ]);

        Assert.Equal("https://example.test/users?page=2", details.Single(detail => detail.Key == "url").Value);
        Assert.Equal("200", details.Single(detail => detail.Key == "response.status").Value);
        Assert.Equal("2", details.Single(detail => detail.Key == "attempt").Value);
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
