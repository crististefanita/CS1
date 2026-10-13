using Atf.Core.Reporting;

namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingClientNavigationTests
{
    private static readonly string[] ClassOrder = ["UsersApiTests", "PostsApiTests"];
    private static readonly string[] ScenarioOrder = ["USERS API — test only"];
    private static readonly string[] TagOrder = ["api", "smoke"];

    [Fact(DisplayName = "Client behaviour opens the overview by default and links category tests to Tests")]
    public void ClientBehaviourOpensOverviewAndLinksCategoryTests()
    {
        var script = ExtentReportClientBehaviour.Build();
        var css = AtfReportTheme.BuildCss();

        Assert.Contains("function initialView()", script, StringComparison.Ordinal);
        Assert.Contains("return 'dashboard-view'", script, StringComparison.Ordinal);
        Assert.Contains("function openInitialView()", script, StringComparison.Ordinal);
        Assert.Contains("toggleView(initialView())", script, StringComparison.Ordinal);
        Assert.Contains("function setActiveNavigation(view)", script, StringComparison.Ordinal);
        Assert.Contains("'dashboard-view': 'nav-dashboard'", script, StringComparison.Ordinal);
        Assert.Contains("function configureCategoryLinks()", script, StringComparison.Ordinal);
        Assert.Contains(".category-view .tag-test-status a.linked[data-atf-test-id]", script, StringComparison.Ordinal);
        Assert.Contains("function installCategoryNavigation()", script, StringComparison.Ordinal);
        Assert.Contains("openTestById(target.getAttribute('data-atf-test-id'))", script, StringComparison.Ordinal);
        Assert.Contains("link.setAttribute('href', '#test-id='", script, StringComparison.Ordinal);
        Assert.Contains("configureCategoryLinks()", script, StringComparison.Ordinal);
        Assert.Contains("installCategoryNavigation()", script, StringComparison.Ordinal);
        Assert.Contains("setTimeout(openInitialView, 0)", script, StringComparison.Ordinal);
        Assert.Contains(".category-view .tag-test-status a.linked", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Exception view publishes API triage fields and a deterministic Tests link")]
    public void ExceptionViewProvidesFailureTriageAndTestLink()
    {
        var script = ExtentReportClientBehaviour.Build();
        var css = AtfReportTheme.BuildCss();

        Assert.Contains("function ensureFailureNavigation()", script, StringComparison.Ordinal);
        Assert.Contains("function failureScenarioRecords()", script, StringComparison.Ordinal);
        Assert.Contains("function reconcileFailureNavigationOrder()", script, StringComparison.Ordinal);
        Assert.Contains("menu.insertBefore(failureItem, overviewItem)", script, StringComparison.Ordinal);
        Assert.Contains("function failureEvidenceRecords(card)", script, StringComparison.Ordinal);
        Assert.Contains("row.dataset.atfAssertionStatus === 'fail'", script, StringComparison.Ordinal);
        Assert.Contains("evidenceId: evidenceId", script, StringComparison.Ordinal);
        Assert.Contains("phaseId: phaseId", script, StringComparison.Ordinal);
        Assert.Contains("requestId: requestId", script, StringComparison.Ordinal);
        Assert.Contains("validationId: validationCardId", script, StringComparison.Ordinal);
        Assert.Contains("assertionId: assertionId", script, StringComparison.Ordinal);
        Assert.Contains("row.dataset.atfEvidenceId = evidence.evidenceId || ''", script, StringComparison.Ordinal);
        Assert.Contains("dataset.atfFailureIds", script, StringComparison.Ordinal);
        Assert.Contains("function requestForValidation(validationCard, phase, card)", script, StringComparison.Ordinal);
        Assert.Contains("function failureIdFor(testId, nodeId, title, ordinal)", script, StringComparison.Ordinal);
        Assert.Contains("data-atf-failure-id", script, StringComparison.Ordinal);
        Assert.Contains("failure-id=", script, StringComparison.Ordinal);
        Assert.Contains("id = 'nav-exception'", script, StringComparison.Ordinal);
        Assert.Contains("fa fa-bug", script, StringComparison.Ordinal);
        Assert.Contains("link.title = 'Fails'", script, StringComparison.Ordinal);
        Assert.Contains("aria-label', 'Fails'", script, StringComparison.Ordinal);
        Assert.Contains(">Fails</span>", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Failures / Bugs", script, StringComparison.Ordinal);
        Assert.Contains("className = 'test-wrapper row view exception-view d-none'", script, StringComparison.Ordinal);
        Assert.Contains("class=\"test-list-tools\"><ul class=\"tools\">", script, StringComparison.Ordinal);
        Assert.Contains("ensureFailureNavigation();", script, StringComparison.Ordinal);
        Assert.Contains(".atf-failure-index", css, StringComparison.Ordinal);
        Assert.Contains(".exception-view .test-list-item .test-item[status=\"fail\"]", css, StringComparison.Ordinal);
        Assert.Contains(".exception-view .test-list-tools", css, StringComparison.Ordinal);
        Assert.Contains("clear: both", css, StringComparison.Ordinal);
        Assert.Contains(".exception-view .test-list-wrapper", css, StringComparison.Ordinal);
        Assert.Contains("function configureFailureView()", script, StringComparison.Ordinal);
        Assert.Contains("data-atf-failure-details", script, StringComparison.Ordinal);
        Assert.Contains("['PHASE', evidence.phase]", script, StringComparison.Ordinal);
        Assert.Contains("['REQUEST', evidence.request]", script, StringComparison.Ordinal);
        Assert.Contains("['VALIDATION', evidence.validation]", script, StringComparison.Ordinal);
        Assert.Contains("['ASSERTION', evidence.assertion", script, StringComparison.Ordinal);
        Assert.Contains("Open in Tests", script, StringComparison.Ordinal);
        Assert.Contains("function renderViewItem(view, listItem)", script, StringComparison.Ordinal);
        Assert.Contains("detailBody.replaceChildren(clone)", script, StringComparison.Ordinal);
        Assert.Contains("renderViewItem(document.querySelector('.test-view'), listItem)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("listItem.click()", script, StringComparison.Ordinal);
        Assert.Contains("window.location.hash = 'test-id='", script, StringComparison.Ordinal);
        Assert.Contains("setTimeout(function ()", script, StringComparison.Ordinal);
        Assert.Contains("global handler clones the source payload into every view", script, StringComparison.Ordinal);
        Assert.Contains("function failureTestCard(nodeId, visibleDetailOnly, failureId)", script, StringComparison.Ordinal);
        Assert.Contains("visibleDetailOnly", script, StringComparison.Ordinal);
        Assert.Contains("Poll only that visible detail pane", script, StringComparison.Ordinal);
        Assert.Contains("candidate.dataset.atfPhase || phaseKey(title)", script, StringComparison.Ordinal);
        Assert.Contains("event.stopImmediatePropagation()", script, StringComparison.Ordinal);
        Assert.Contains("const activeView = document.querySelector('.main-content > .' + view)", script, StringComparison.Ordinal);
        Assert.Contains("renderFirstViewItem(activeView)", script, StringComparison.Ordinal);
        Assert.Contains(".atf-failure-schematic", css, StringComparison.Ordinal);
        Assert.Contains(".atf-failure-open", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Tests and Fails navigation rows align status beside the identity")]
    public void NavigationRowsKeepStatusBesideIdentity()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(
            ".test-view .test-list-wrapper > .test-list-item > .test-item,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".exception-view .test-list-wrapper > .test-list-item > .test-item {",
            css,
            StringComparison.Ordinal);
        Assert.Contains("display: flex !important;", css, StringComparison.Ordinal);
        Assert.Contains("align-items: center !important;", css, StringComparison.Ordinal);
        Assert.Contains("flex: 1 1 auto !important;", css, StringComparison.Ordinal);
        Assert.Contains(
            ".test-view .test-list-wrapper > .test-list-item > .test-item > .test-detail > .text-sm > .badge",
            css,
            StringComparison.Ordinal);
        Assert.Contains("position: absolute !important;", css, StringComparison.Ordinal);
        Assert.Contains("top: 0 !important;", css, StringComparison.Ordinal);
        Assert.Contains("right: 0 !important;", css, StringComparison.Ordinal);
        Assert.Contains("margin-left: auto !important;", css, StringComparison.Ordinal);
        Assert.Contains("white-space: nowrap !important;", css, StringComparison.Ordinal);

        var rowStart = css.IndexOf(
            ".test-view .test-list-wrapper > .test-list-item > .test-item,",
            StringComparison.Ordinal);
        var detailStart = css.IndexOf(
            ".test-view .test-list-wrapper > .test-list-item > .test-item > .test-detail,",
            StringComparison.Ordinal);
        var statusStart = css.IndexOf(
            ".test-view .test-list-wrapper > .test-list-item > .test-item > .test-status,",
            StringComparison.Ordinal);

        Assert.True(rowStart >= 0 && detailStart > rowStart && statusStart > detailStart);
    }

    [Fact(DisplayName = "Failure projection uses immutable typed records and a stable index")]
    public void FailureProjectionUsesTypedRecordsAndStableIndex()
    {
        var script = ExtentReportClientFailureProjectionFragments.Build();
        var evidence = ExtentReportClientFailureEvidenceFragments.Build();

        Assert.Contains("class FailureRecord", script, StringComparison.Ordinal);
        Assert.Contains("class FailureIndex", script, StringComparison.Ordinal);
        Assert.Contains("function collectFailureCandidates()", script, StringComparison.Ordinal);
        Assert.Contains("function projectFailureRecords(candidates)", script, StringComparison.Ordinal);
        Assert.Contains("return projectFailureRecords(collectFailureCandidates())", script, StringComparison.Ordinal);
        Assert.Contains("Object.freeze(this)", script, StringComparison.Ordinal);
        Assert.Contains("this.records = Object.freeze(Array.from(records || []))", script, StringComparison.Ordinal);
        Assert.Contains("return new FailureRecord({", script, StringComparison.Ordinal);
        Assert.Contains("const records = (candidates || []).map", script, StringComparison.Ordinal);
        Assert.Contains("return new FailureIndex(records)", script, StringComparison.Ordinal);
        Assert.Contains("if (!failureIds.includes(failureId)) failureIds.push(failureId);", script, StringComparison.Ordinal);
        Assert.Contains("deepLink:", script, StringComparison.Ordinal);
        Assert.Contains("expected: failure.evidence.expected || ''", script, StringComparison.Ordinal);
        Assert.Contains("actual: failure.evidence.actual || ''", script, StringComparison.Ordinal);
        Assert.Contains("expected: cells[1] || ''", evidence, StringComparison.Ordinal);
        Assert.Contains("actual: cells[2] || ''", evidence, StringComparison.Ordinal);
        Assert.True(
            script.IndexOf("function collectFailureCandidates()", StringComparison.Ordinal)
                < script.IndexOf("function projectFailureRecords(candidates)", StringComparison.Ordinal),
            "DOM discovery must precede the pure typed projection boundary.");
        var projectorBody = script[
            script.IndexOf("function projectFailureRecords(candidates)", StringComparison.Ordinal)..
            script.IndexOf("function failureScenarioRecords()", StringComparison.Ordinal)];
        Assert.DoesNotContain("querySelector", projectorBody, StringComparison.Ordinal);
        Assert.DoesNotContain("return {\n                testId: testId", script, StringComparison.Ordinal);
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
            $"const classOrder = {System.Text.Json.JsonSerializer.Serialize(ClassOrder)}",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            $"const scenarioOrder = {System.Text.Json.JsonSerializer.Serialize(ScenarioOrder)}",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            $"const tagOrder = {System.Text.Json.JsonSerializer.Serialize(TagOrder)}",
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
        Assert.Contains("else if (status.includes('warning')) totals.other++", script, StringComparison.Ordinal);
        Assert.Contains("const total = data.scenarioCards.length", script, StringComparison.Ordinal);
        Assert.Contains("['Total tests warning', scenarioStatusTotals.other]", script, StringComparison.Ordinal);
        Assert.Contains("'</b> warnings'", script, StringComparison.Ordinal);
        Assert.Contains("replace(/\\bothers\\b/gi, 'warnings')", script, StringComparison.Ordinal);
        Assert.Contains("setText(summaryValue('Tests Passed'), totals.passed)", script, StringComparison.Ordinal);
        Assert.Contains("setText(summaryValue('Tests Failed'), totals.failed)", script, StringComparison.Ordinal);
        Assert.Contains("statusGroup.parentCount = total", script, StringComparison.Ordinal);
        Assert.Contains("statusGroup.warningParent = totals.other", script, StringComparison.Ordinal);
        Assert.Contains("function removeExecutionPhaseChart(dashboard)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("removeLogEventsChart", script, StringComparison.Ordinal);
        Assert.DoesNotContain("events-analysis", script, StringComparison.Ordinal);
        Assert.Contains("function updateOverviewCharts(totals)", script, StringComparison.Ordinal);
        Assert.Contains("dataset.data = [totals.passed, totals.failed, totals.other, totals.skipped]", script, StringComparison.Ordinal);
        Assert.DoesNotContain("function labelOverviewMetrics(dashboard)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("title.textContent = 'Execution phases'", script, StringComparison.Ordinal);
        Assert.DoesNotContain("phases passed", script, StringComparison.Ordinal);
        Assert.DoesNotContain("phases failed", script, StringComparison.Ordinal);
        Assert.Contains("const nativeTagTable = dashboard.querySelector('.category-container table')", script, StringComparison.Ordinal);
        Assert.Contains("headerCells[4].textContent = 'WARNING'", script, StringComparison.Ordinal);
        Assert.Contains("const tagTotal = totals.passed + totals.failed + totals.skipped + totals.other", script, StringComparison.Ordinal);
        Assert.Contains("status.includes('warning') ? 'other'", script, StringComparison.Ordinal);
        Assert.Contains("cells[5].textContent = Math.round(totals.passed / Math.max(tagTotal, 1) * 100) + '%'", script, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_TEST_TOTAL__", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Overview charts use one semantic palette and remove structural execution phases")]
    public void OverviewChartsUseSemanticPaletteAndOmitExecutionPhases()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("const chartPalette = {", script, StringComparison.Ordinal);
        Assert.Contains("chartPalette.pass", script, StringComparison.Ordinal);
        Assert.Contains("chartPalette.fail", script, StringComparison.Ordinal);
        Assert.Contains("chartPalette.warning", script, StringComparison.Ordinal);
        Assert.Contains("chartPalette.skip", script, StringComparison.Ordinal);
        Assert.Contains("chartPalette.info", script, StringComparison.Ordinal);
        Assert.Contains(AtfReportColors.OverviewPass, script, StringComparison.Ordinal);
        Assert.Contains(AtfReportColors.OverviewFail, script, StringComparison.Ordinal);
        Assert.Contains(AtfReportColors.OverviewSkip, script, StringComparison.Ordinal);
        Assert.Contains("dataset.borderWidth = 2", script, StringComparison.Ordinal);
        Assert.Contains("dashboard.querySelector('#grandchild-analysis')?.closest('.card')", script, StringComparison.Ordinal);
        Assert.Contains("card.closest('.col-md-3')", script, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_CHART_PALETTE__", script, StringComparison.Ordinal);
        Assert.DoesNotContain("canvasId === 'events-analysis'", script, StringComparison.Ordinal);

        var colors = new[]
        {
            AtfReportColors.OverviewPass,
            AtfReportColors.OverviewFail,
            AtfReportColors.OverviewWarning,
            AtfReportColors.OverviewSkip,
            AtfReportColors.OverviewInfo,
            AtfReportColors.OverviewOther
        };
        Assert.Equal(colors.Length, colors.Distinct(StringComparer.OrdinalIgnoreCase).Count());
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

    [Fact(DisplayName = "Client behaviour exposes a read-only DOM contract harness")]
    public void ClientBehaviourEmbedsDomContractHarness()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.Contains("<script id=\"atf-dom-contract\">", script, StringComparison.Ordinal);
        Assert.Contains("window.__atfDomContract", script, StringComparison.Ordinal);
        Assert.Contains("version: 1", script, StringComparison.Ordinal);
        Assert.Contains("inspect: function ()", script, StringComparison.Ordinal);
        Assert.Contains("statusBesideIdentity", script, StringComparison.Ordinal);
        Assert.Contains("openEvidenceCount", script, StringComparison.Ordinal);
        Assert.Contains("overflowY", script, StringComparison.Ordinal);
        Assert.Contains("Object.freeze({", script, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_DOM_CONTRACT_CONTROLLER__", script, StringComparison.Ordinal);
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
