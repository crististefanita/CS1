namespace Atf.Reporting.Extent.Theme.Css;

/// <summary>
/// CSS fragments with an independent semantic owner. The main theme remains
/// the compatibility façade for Extent's global surface rules; execution-tree
/// styling lives here so hierarchy changes cannot silently modify overview CSS.
/// </summary>
internal static class AtfReportThemeFragments
{
    public static string BuildExecutionHierarchyCss() => """
       /* Execution hierarchy contract: TEST BODY is the phase shell; each HTTP
          request and its validation are direct sibling cards in execution order. */
      .test-view .test-content-detail .card[data-atf-node-role="phase"] > .accordion[data-atf-node-group="phase-children"] {
        display: block !important;
        margin: 6px 12px 8px !important;
        padding: 0 !important;
        border: 0 !important;
        background: transparent !important;
      }
      .test-view .test-content-detail .card[data-atf-node-role="request"],
      .test-view .test-content-detail .card[data-atf-node-role="validation"] {
        margin: 0 0 10px !important;
        border: 1px solid var(--atf-border) !important;
        border-radius: 8px !important;
        background: var(--atf-bg) !important;
        box-shadow: 0 3px 10px rgba(0,0,0,.12) !important;
        overflow: hidden !important;
      }
      .test-view .test-content-detail .card[data-atf-node-role="request"] > .card-header {
        background: var(--atf-panel-2) !important;
        border-bottom: 1px solid var(--atf-border) !important;
      }
      .test-view .test-content-detail .card[data-atf-node-role="request"] > .collapse,
      .test-view .test-content-detail .card[data-atf-node-role="validation"] > .collapse {
        margin: 0 !important;
        border: 0 !important;
        background: var(--atf-bg) !important;
      }
      .test-view .test-content-detail .card[data-atf-node-role="request"] > .collapse > .card-body,
      .test-view .test-content-detail .card[data-atf-node-role="validation"] > .collapse > .card-body {
        padding: 10px 12px 4px !important;
        background: var(--atf-bg) !important;
      }
      .test-view .test-content-detail .card[data-atf-node-role="validation"] > .card-header {
        min-height: 0 !important;
        padding: 7px 10px !important;
        background: var(--atf-panel-2) !important;
        border-bottom: 1px solid var(--atf-border) !important;
      }
    """;
}
