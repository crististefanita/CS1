namespace Atf.Reporting.Extent.Theme;

/// <summary>Execution hierarchy, lifecycle shells and native table foundations.</summary>
internal static class AtfReportThemeExecutionFragments
{
    public static string Build() => """
      .test-contents > .mt-4 > .accordion > .card {
        background: transparent !important;
        border-color: var(--atf-border) !important;
        border-radius: 0 !important;
        box-shadow: 0 10px 26px __ATF_SHADOWSTRONG__ !important;
      }
      .test-contents > .mt-4 > .accordion > .card > .card-header { background: transparent !important; }
      .test-contents > .mt-4 > .accordion > .card .node {
        background: transparent !important;
        border: 0 !important;
        border-radius: 0 !important;
        box-shadow: none !important;
        margin: 0 !important;
      }
      /* Extent applies a muted status colour to container nodes. API
         scenario and lifecycle titles are primary navigation, not metadata;
         keep them readable and consistent with request titles. */
      .test-view .test-content-detail .card > .card-header .card-title .node {
        color: var(--atf-text) !important;
        font-family: var(--atf-font-sans) !important;
      }
    
      /* Lifecycle phases are containers, not ordinary API steps. Give
         SETUP / TEST BODY / TEARDOWN a visible frame and indent their
         request/assertion children so the execution order is readable at
         a glance after the scenario is expanded. */
      .test-view .test-content-detail .card[data-atf-phase] {
        margin: 0 0 14px !important;
        border: 1px solid var(--atf-border) !important;
        border-left: 3px solid var(--atf-accent) !important;
        border-radius: 8px !important;
        background: var(--atf-panel) !important;
        box-shadow: 0 6px 16px __ATF_SHADOWSOFT__ !important;
      }
      .test-view .test-content-detail .card[data-atf-phase="setup"] { border-left-color: __ATF_SETUP__ !important; }
      .test-view .test-content-detail .card[data-atf-phase="test-body"] { border-left-color: var(--atf-accent) !important; }
      .test-view .test-content-detail .card[data-atf-phase="teardown"] { border-left-color: var(--atf-good) !important; }
      .test-view .test-content-detail .card[data-atf-phase] > .card-header {
        min-height: 44px !important;
        padding: 10px 14px !important;
        background: __ATF_ACCENT045__ !important;
        border-bottom: 1px solid var(--atf-border) !important;
      }
      .test-view .test-content-detail .card[data-atf-phase] > .accordion {
        margin: 10px 14px 14px !important;
        padding-left: 12px !important;
         /* The phase card already provides the semantic left accent. Do
            not draw a second vertical rail inside TEST BODY content. */
         border-left: 0 !important;
      }
      .test-view .test-content-detail .card[data-atf-phase] > .accordion > .card {
        margin: 8px 0 !important;
        border-radius: 6px !important;
        box-shadow: none !important;
      }
      .test-view .test-content-detail .card[data-atf-phase] > .accordion > .card > .card-header {
        background: __ATF_WHITE018__ !important;
      }

      .node-info, .event-row td { color: var(--atf-text) !important; }
      .event-row { border-color: var(--atf-border) !important; }
      .event-row td { background: var(--atf-bg) !important; border-color: var(--atf-border) !important; }
      .table, .table tbody, .table tr, .table td, .table th { background: var(--atf-bg) !important; border-color: var(--atf-border) !important; color: var(--atf-text) !important; }
      .table th { color: var(--atf-muted) !important; }
      /* Native dark mode applies #ddd to every .table cell. The reference
         report keeps audit table values on the brighter report text token. */
      body.dark .atf-tag-table, body.dark .atf-tag-table tbody, body.dark .atf-tag-table tr, body.dark .atf-tag-table td { color: var(--atf-text) !important; }
    
      /* Native Extent status badges: retain the compact reference badge
         silhouette and its lighter semantic palette. Custom pill.ok/.fail
         evidence markers remain intentionally separate below. */
      .badge-success, .pass-bg, .status.success { background: __ATF_EXTENTPASS__ !important; color: __ATF_WHITE__ !important; }
      .badge-warning, .skip-bg, .status.warning { background: __ATF_EXTENTSKIP__ !important; color: __ATF_WHITE__ !important; }
      .badge-danger, .fail-bg { background: __ATF_EXTENTFAIL__ !important; color: __ATF_WHITE__ !important; }
      .text-pass { color: var(--atf-good) !important; }
      .text-skip { color: var(--atf-warn) !important; }
      .text-fail { color: var(--atf-bad) !important; }
    
 
    """;
}

