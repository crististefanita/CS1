using Atf.Core.Reporting;
using AventStack.ExtentReports;

namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingComponentTests
{
    private static readonly string[] ClassOrder = ["UsersApiTests", "PostsApiTests"];
    private static readonly string[] ScenarioOrder = ["USERS API — test only"];
    private static readonly string[] TagOrder = ["api", "smoke"];

    [Fact(DisplayName = "Report theme expands the central palette without leaking placeholders")]
    public void ReportThemeUsesCentralPalette()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(AtfReportColors.Dark, css, StringComparison.Ordinal);
        Assert.Contains(AtfReportColors.JsonSurface, css, StringComparison.Ordinal);
        Assert.Contains($"--atf-text: {AtfReportColors.Text}", css, StringComparison.Ordinal);
        Assert.Contains(AtfReportColors.SecondarySectionHeadingBackground, css, StringComparison.Ordinal);
        Assert.Contains("--atf-secondary-heading-text: var(--atf-accent)", css, StringComparison.Ordinal);
        Assert.Contains("atf-tag-card .card-header", css, StringComparison.Ordinal);
        Assert.Contains(".atf-tag-card .card-header", css, StringComparison.Ordinal);
        Assert.Contains("color: var(--atf-secondary-heading-text) !important", css, StringComparison.Ordinal);
        Assert.Contains("font-family: var(--atf-overview-font-family) !important", css, StringComparison.Ordinal);
        Assert.Contains("font-size: var(--atf-font-base-size) !important", css, StringComparison.Ordinal);
        Assert.Contains("body.dark .atf-tag-table, body.dark .atf-tag-table tbody, body.dark .atf-tag-table tr, body.dark .atf-tag-table td { color: var(--atf-text) !important; }", css, StringComparison.Ordinal);
        Assert.Contains("--atf-dark: #101723", css, StringComparison.Ordinal);
        Assert.Contains(".test-wrapper .test-list-wrapper > .test-list-item > .test-item > .test-contents", css, StringComparison.Ordinal);
        Assert.Contains("display: none !important", css, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_", css, StringComparison.Ordinal);
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

    [Fact(DisplayName = "Exception view publishes API triage fields and a deterministic Tests link")]
    public void ExceptionViewProvidesFailureTriageAndTestLink()
    {
        var script = ExtentReportClientBehaviour.Build();
        var css = AtfReportTheme.BuildCss();

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
        Assert.Contains("function failureTestCard(nodeId, visibleDetailOnly)", script, StringComparison.Ordinal);
        Assert.Contains("visibleDetailOnly", script, StringComparison.Ordinal);
        Assert.Contains("Poll only that visible detail pane", script, StringComparison.Ordinal);
        Assert.Contains("candidate.dataset.atfPhase || phaseKey(title)", script, StringComparison.Ordinal);
        Assert.Contains("event.stopImmediatePropagation()", script, StringComparison.Ordinal);
        Assert.Contains("const activeView = document.querySelector('.main-content > .' + view)", script, StringComparison.Ordinal);
        Assert.Contains("renderFirstViewItem(activeView)", script, StringComparison.Ordinal);
        Assert.Contains(".atf-failure-schematic", css, StringComparison.Ordinal);
        Assert.Contains(".atf-failure-open", css, StringComparison.Ordinal);
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
        Assert.Contains("const total = data.scenarioCards.length", script, StringComparison.Ordinal);
        Assert.Contains("setText(summaryValue('Tests Passed'), totals.passed)", script, StringComparison.Ordinal);
        Assert.Contains("setText(summaryValue('Tests Failed'), totals.failed)", script, StringComparison.Ordinal);
        Assert.Contains("statusGroup.parentCount = total", script, StringComparison.Ordinal);
        Assert.Contains("function removeExecutionPhaseChart(dashboard)", script, StringComparison.Ordinal);
        Assert.Contains("function removeLogEventsChart(dashboard)", script, StringComparison.Ordinal);
        Assert.Contains("removeLogEventsChart(dashboard)", script, StringComparison.Ordinal);
        Assert.Contains("dashboard.querySelector('#events-analysis')", script, StringComparison.Ordinal);
        Assert.Contains("chart.destroy?.()", script, StringComparison.Ordinal);
        Assert.Contains("function updateOverviewCharts(totals)", script, StringComparison.Ordinal);
        Assert.Contains("dataset.data = [totals.passed, totals.failed, totals.other, totals.skipped]", script, StringComparison.Ordinal);
        Assert.DoesNotContain("function labelOverviewMetrics(dashboard)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("title.textContent = 'Execution phases'", script, StringComparison.Ordinal);
        Assert.DoesNotContain("phases passed", script, StringComparison.Ordinal);
        Assert.DoesNotContain("phases failed", script, StringComparison.Ordinal);
        Assert.Contains("const nativeTagTable = dashboard.querySelector('.category-container table')", script, StringComparison.Ordinal);
        Assert.Contains("const tagTotal = totals.passed + totals.failed + totals.skipped + totals.other", script, StringComparison.Ordinal);
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

    [Fact(DisplayName = "Tag audit removes redundant explanatory copy")]
    public void ClientBehaviourOmitsRedundantTagAuditDescription()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.DoesNotContain("Deduplicated API navigation metadata", script, StringComparison.Ordinal);
        Assert.DoesNotContain("atf-tag-audit-heading", script, StringComparison.Ordinal);
        Assert.Contains("ATF scenario identities by test class", script, StringComparison.Ordinal);
        Assert.Contains("Test packages", script, StringComparison.Ordinal);
        Assert.Contains("Other annotations / tags", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Tag audit headings share native Overview typography and colour")]
    public void ReportThemeUsesNativeOverviewHeadingContractForTagAuditCards()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(".dashboard-view .category-container > .card > .card-header p", css, StringComparison.Ordinal);
        Assert.Contains(".dashboard-view .sysenv-container > .card > .card-header p", css, StringComparison.Ordinal);
        Assert.Contains(".dashboard-view .atf-tag-card > .card-header p { margin: 0 !important; color: var(--atf-secondary-heading-text) !important; font-family: var(--atf-overview-font-family) !important; font-size: var(--atf-font-base-size) !important;", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".atf-tag-audit-heading", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Category status badges share one fixed height")]
    public void CategoryStatusBadgesUseOneHeight()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains("--atf-category-badge-height: 22px;", css, StringComparison.Ordinal);
        Assert.Contains(
            ".category-view .badge.log {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "height: var(--atf-category-badge-height) !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "min-height: var(--atf-category-badge-height) !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "align-items: center !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "line-height: 1 !important;",
            css,
            StringComparison.Ordinal);
        Assert.DoesNotContain("\n      .badge.log {", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Tag audit cards use the native Tags and System Environment frame")]
    public void ReportThemeUsesNativeOverviewFrameForTagAuditCards()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(
            ".atf-tag-audit-disclosure {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border: 0 !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".atf-tag-card { min-width: 0 !important; border: 0 !important; border-radius: 0 !important; background: transparent !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .atf-tag-card { min-width: 0 !important; border: 0 !important; border-radius: 0 !important; background: transparent !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".atf-tag-card .card-header { min-height: 0 !important; padding: 14px 24px 16px !important; border: 1px solid var(--atf-border) !important; border-bottom: 0 !important; border-radius: 8px 8px 0 0 !important; background: var(--atf-secondary-heading-bg) !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .atf-tag-card > .card-header p { margin: 0 !important; color: var(--atf-secondary-heading-text) !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".atf-tag-scroll { height: var(--atf-audit-scroll-height) !important; max-height: var(--atf-audit-scroll-height) !important; overflow-x: hidden !important; overflow-y: auto !important; border: 1px solid var(--atf-border) !important; border-top: 0 !important; border-radius: 0 0 8px 8px !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".atf-tag-table { width: 100% !important; margin: 0 !important; table-layout: fixed !important; border: 0 !important;",
            css,
            StringComparison.Ordinal);
        Assert.DoesNotContain(".atf-tag-total", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Native Tags and System Environment tables reuse the audit panel cell contract")]
    public void NativeOverviewTablesUseSinglePanelAndAuditCellContract()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(
            ".dashboard-view .category-container > .card,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "min-width: 0 !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "background: transparent !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .category-container > .card > .card-header,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card > .card-header {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border: 1px solid var(--atf-border) !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border-bottom: 0 !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .category-container > .card > .card-body,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card > .card-body {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border-top: 0 !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border-radius: 0 0 8px 8px !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "overflow-y: auto !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .category-container > .card > .card-body > table,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card > .card-body > table {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "table-layout: fixed !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .category-container > .card > .card-body > table th,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card > .card-body > table td {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "padding: 7px 9px !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "font-size: var(--atf-overview-font-size) !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "line-height: 1.32 !important;",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .atf-tag-card .atf-tag-table > thead > tr > th,",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".dashboard-view .sysenv-container > .card > .card-body > table > tbody > tr > th {",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            "border-color: var(--atf-border) !important;",
            css,
            StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Tag audit tables contain only a header and data rows")]
    public void ClientBehaviourDoesNotAddSyntheticAuditTotalRows()
    {
        var script = ExtentReportClientBehaviour.Build();

        Assert.DoesNotContain("atf-tag-total", script, StringComparison.Ordinal);
        Assert.DoesNotContain("ATF scenario identities total", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Test packages total", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Other annotations / tags total", script, StringComparison.Ordinal);
        Assert.Contains("buildAuditCard('classes', 'ATF scenario identities by test class'", script, StringComparison.Ordinal);
        Assert.Contains("buildAuditCard('packages', 'Test packages'", script, StringComparison.Ordinal);
        Assert.Contains("buildAuditCard('other', 'Other annotations / tags'", script, StringComparison.Ordinal);
    }

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
        Assert.Contains("targetHeader?.__atfToggleHandler", script, StringComparison.Ordinal);
        Assert.Contains("body.style.setProperty('display', expanded ? 'block' : 'none', 'important');", script, StringComparison.Ordinal);
        Assert.DoesNotContain("document.addEventListener('click', toggleCard, true)", script, StringComparison.Ordinal);
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
