using System.Text.Json;

namespace Atf.Reporting.Extent.Theme;

/// <summary>
/// Facade that composes independent browser controllers and report-specific
/// behaviours into the self-contained Extent document.
/// </summary>
internal static class ExtentReportClientBehaviour
{
    public static string Build(ReportingSettings? settings = null)
    {
        var classOrder = JsonSerializer.Serialize(settings?.TestClassOrder ?? []);
        var scenarioOrder = JsonSerializer.Serialize(settings?.TestScenarioOrder ?? []);
        var tagOrder = JsonSerializer.Serialize(settings?.TagOrder ?? []);
        var chartPalette = JsonSerializer.Serialize(new
        {
            pass = AtfReportColors.OverviewPass,
            fail = AtfReportColors.OverviewFail,
            warning = AtfReportColors.OverviewWarning,
            skip = AtfReportColors.OverviewSkip,
            info = AtfReportColors.OverviewInfo,
            other = AtfReportColors.OverviewOther
        });

        var script = """
        <script id="atf-custom-behaviour">
          (function () {
            const chartPalette = __ATF_CHART_PALETTE__;

            __ATF_NAVIGATION_CONTROLLER__
            __ATF_DISCLOSURE_CONTROLLER__
            __ATF_FAILURE_BEHAVIOUR__
            __ATF_CONFIGURE_EXECUTION_HIERARCHY__

            function removeCompositeScenarioSummary() {
              // A lifecycle scenario owns the meaningful SETUP / TEST BODY /
              // TEARDOWN cards. Remove Extent's generic parent event table so
              // the execution order is represented once.
              document.querySelectorAll('.test-view .test-content-detail .card').forEach(function (card) {
                const accordion = directChild(card, 'accordion');
                if (!accordion) return;

                const hasPhaseChild = Array.from(accordion.children).some(function (child) {
                  const header = directChild(child, 'card-header');
                  const title = header && header.querySelector('.card-title .node');
                  return phaseKey(title && title.textContent) !== null;
                });
                if (!hasPhaseChild) return;

                const summary = directChild(card, 'collapse');
                if (summary) summary.remove();
                card.dataset.atfCompositeSummaryRemoved = 'true';
              });
            }

            __ATF_OVERVIEW_BEHAVIOUR__
            function configure() {
              configureNavigationOrder();
              configurePhaseCards();
              configureExecutionHierarchy();
              ensureFailureNavigation();
              installFailureNavigation();
              configureFailureView();
              configureCategoryLinks();
              removeCompositeScenarioSummary();
              updateNativeOverview();
              document.querySelectorAll('.test-view .accordion > .card').forEach(function (card) {
                const header = directChild(card, 'card-header');
                const bodies = cardBodies(card);
                if (!header || bodies.length === 0) return;
                if (card.dataset.atfExpansionConfigured === 'true') return;
                card.dataset.atfExpansionConfigured = 'true';
                header.setAttribute('role', 'button');
                header.setAttribute('tabindex', '0');
                bodies.forEach(function (body) {
                  // Scenario navigation remains compact; phase shells and API
                  // evidence open only when the user explicitly clicks them.
                  body.classList.remove('show');
                  body.style.setProperty('display', 'none', 'important');
                });
                header.setAttribute('aria-expanded', 'false');
                bindCardToggle(card, header);
              });

              document.querySelectorAll('.test-view details.step-detail').forEach(function (detail) {
                if (detail.dataset.atfDefaultCollapsed === 'true') return;
                detail.open = false;
                detail.dataset.atfDefaultCollapsed = 'true';
              });
              buildTagAudit();
            }

            function start() {
              installNavigation();
              installCategoryNavigation();
              installCardDelegation();
              configure();
              setTimeout(openInitialView, 0);
              const root = document.querySelector('.test-view') || document.body;
              new MutationObserver(configure).observe(root, { childList: true, subtree: true });
              [50, 250, 750].forEach(function (delay) { setTimeout(configure, delay); });
            }

            if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
            else start();
          }());
        </script>
        """;

        return script
            .Replace(
                "__ATF_NAVIGATION_CONTROLLER__",
                ExtentReportClientNavigationController.Build()
                    .Replace("__ATF_CLASS_ORDER__", classOrder, StringComparison.Ordinal)
                    .Replace("__ATF_SCENARIO_ORDER__", scenarioOrder, StringComparison.Ordinal)
                    .Replace("__ATF_TAG_ORDER__", tagOrder, StringComparison.Ordinal),
                StringComparison.Ordinal)
            .Replace(
                "__ATF_DISCLOSURE_CONTROLLER__",
                ExtentReportClientDisclosureController.Build(),
                StringComparison.Ordinal)
            .Replace(
                "__ATF_OVERVIEW_BEHAVIOUR__",
                ExtentReportClientOverviewFragments.Build(),
                StringComparison.Ordinal)
            .Replace(
                "__ATF_FAILURE_BEHAVIOUR__",
                ExtentReportClientFailureFragments.Build(),
                StringComparison.Ordinal)
            .Replace(
                "__ATF_CONFIGURE_EXECUTION_HIERARCHY__",
                ExtentReportClientFragments.BuildExecutionHierarchyFunction(),
                StringComparison.Ordinal)
            .Replace("__ATF_CHART_PALETTE__", chartPalette, StringComparison.Ordinal);
    }
}
