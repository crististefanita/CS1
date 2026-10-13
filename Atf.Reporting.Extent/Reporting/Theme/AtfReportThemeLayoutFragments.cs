namespace Atf.Reporting.Extent.Theme;

/// <summary>Shared surface, navigation and exception-view layout rules.</summary>
internal static class AtfReportThemeLayoutFragments
{
    public static string Build() => """
      :root {
        /* Single dark surface token: every report surface resolves to this value. */
        --atf-dark: __ATF_DARK__;
        /* JSON evidence follows the reference RegressionExtent surface. */
        --atf-json-surface: __ATF_JSONSURFACE__;
        --atf-json-text: __ATF_JSONTEXT__;
        --atf-bg: var(--atf-dark);
        --atf-panel: var(--atf-dark);
        --atf-panel-2: var(--atf-dark);
        --atf-panel-3: var(--atf-dark);
        --atf-border: __ATF_BORDER__;
        --atf-reference-border: __ATF_REFERENCEBORDER__;
        --atf-text: __ATF_TEXT__;
        --atf-muted: __ATF_MUTED__;
        --atf-accent: __ATF_ACCENT__;
        /* Shared secondary-section heading tokens. These are intentionally
           reused by ATF metadata, Tags and System/Environment so the
           dashboard reads as one coherent information layer. */
        --atf-secondary-heading-bg: __ATF_ACCENT07__;
        --atf-secondary-heading-bg-hover: __ATF_ACCENT12__;
        --atf-secondary-heading-text: var(--atf-accent);
        --atf-secondary-heading-border: __ATF_ACCENT42__;
        --atf-good: __ATF_GOOD__;
        --atf-warn: __ATF_WARNING__;
        --atf-bad: __ATF_DANGER__;
        /* Overview support text uses the report font at 90% so metadata
           reads as secondary without introducing a second type system. */
        --atf-font-sans: __ATF_FONT_SANS__;
        --atf-font-code: __ATF_FONT_CODE__;
        --atf-font-base-size: __ATF_FONT_BASE_SIZE__;
        --atf-overview-font-family: var(--atf-font-sans);
        --atf-overview-font-size: __ATF_FONT_OVERVIEW_SIZE__;
        --atf-json-font-size: __ATF_FONT_JSON_SIZE__;
        --atf-table-header-font-size: __ATF_FONT_TABLE_HEADER_SIZE__;
        --atf-table-cell-font-size: __ATF_FONT_TABLE_CELL_SIZE__;
        --atf-badge-font-size: __ATF_FONT_BADGE_SIZE__;
        --atf-category-badge-height: 22px;
        /* One shared viewport for audit tables and JSON code blocks. */
        --atf-audit-scroll-height: 340px;
      }

      html, body, body.dark {
        color: var(--atf-text) !important;
        background: var(--atf-bg) !important;
        font-family: var(--atf-font-sans) !important;
      }

      .header.navbar, .vheader {
        background: var(--atf-bg) !important;
        border-bottom: 1px solid var(--atf-border) !important;
      }
      .header.navbar { box-shadow: 0 8px 30px __ATF_SHADOWSOFT__; }
      .nav-right .badge-primary, .badge-primary {
        background: var(--atf-accent) !important;
        color: __ATF_ACCENTCONTRAST__ !important;
        border: 0 !important;
        font-weight: 700 !important;
      }
      .side-nav {
        background: var(--atf-bg) !important;
        border-right: 1px solid var(--atf-border) !important;
      }
      .side-nav .nav-item a { color: var(--atf-muted) !important; }
      .side-nav .nav-item a:hover, .side-nav .nav-item.active a { color: var(--atf-text) !important; background: __ATF_ACCENT12__ !important; }
      .dark .side-nav,
      .dark .test-list,
      .dark .side-nav .side-nav-inner .side-nav-menu {
        background: var(--atf-bg) !important;
        border-right-color: var(--atf-border) !important;
      }
      .vcontainer, .main-content, .test-wrapper, .dashboard-view, .category-view { background: var(--atf-bg) !important; }
    
      .test-list {
        background: var(--atf-bg) !important;
        border-right: 1px solid var(--atf-border) !important;
      }
      .test-list-tools, .test-list-wrapper { background: transparent !important; }
      .test-list-tools { border-bottom: 1px solid var(--atf-border) !important; }
      .test-content { background: var(--atf-bg) !important; border-color: var(--atf-border) !important; box-shadow: 0 10px 26px __ATF_SHADOWSTRONG__ !important; }
      .test-list-item .test-item {
        background: var(--atf-bg) !important;
        border-bottom: 1px solid var(--atf-border) !important;
        transition: background .15s ease, border-color .15s ease;
      }
      .test-list-item .test-item:hover { background: __ATF_ACCENT08__ !important; }
      .test-list-item .test-item[status="pass"] { border-left: 3px solid var(--atf-good) !important; }
      .test-list-item .test-item[status="skip"] { border-left: 3px solid var(--atf-warn) !important; }
      .test-list-item .test-item[status="fail"] { border-left: 3px solid var(--atf-bad) !important; }
      .test-detail .name, .test-status { color: var(--atf-text) !important; }
      .test-detail .text-sm, .text-sm, .text-muted { color: var(--atf-muted) !important; }
      .test-contents { background: transparent !important; }
      /* Extent keeps the source payload inside each navigation item and
         clones it into the view's right-hand detail pane. The source payload
         must never become a second navigation tree when an item is selected. */
      .test-wrapper .test-list-wrapper > .test-list-item > .test-item > .test-contents {
        display: none !important;
      }
      .test-wrapper, .test-content, .test-content-detail { overflow-x: hidden !important; }
    
      /* Categories are already rendered once in the test header. Extent
         propagates them to every node, so suppress all inherited copies
         inside the execution tree. */
      .test-view .node-attr { display: none !important; }
    
      .detail-head { background: transparent !important; border: 0 !important; border-radius: 0 !important; box-shadow: none !important; padding: 0 !important; margin: 0 !important; }
      .test-content-detail {
        background: var(--atf-bg) !important;
        border: 0 !important;
        border-radius: 0 !important;
        box-shadow: none !important;
      }
    
      /* Test details: keep the event table focused on status and
         evidence. Remove Extent's Timestamp column everywhere in test
         details, without affecting dashboard Started/Ended metadata. */
      .test-view .detail-body > table thead th.timestamp-col,
      .test-view .detail-body > table > tbody > tr > td:nth-child(2) {
        display: none !important;
      }
      .test-view .detail-body > table thead th.status-col {
        width: 88px !important;
      }
      .test-view .detail-body > table thead th.details-col {
        width: auto !important;
      }
    
      /* API triage is duration-first: do not make the reader decode
         duplicated wall-clock timestamps. Keep the measured duration in
         the test list, node header and request/response trace tables. */
      .test-view .test-list .test-detail p.text-sm {
        font-size: 0 !important;
      }
      .test-view .test-list .test-detail p.text-sm > span:first-child {
        display: none !important;
      }
      .test-view .test-list .test-detail p.text-sm > span:nth-of-type(2) {
        font-size: .75rem !important;
      }
      .test-view .detail-head .info > span.badge-success,
      .test-view .detail-head .info > span.badge-danger:not(.pointer) {
        display: none !important;
      }
      .test-view .detail-head .info > span.badge-default {
        margin-left: 0 !important;
      }

      /* Exception view is an index, not a second execution model. Keep the
         native grouping/status, then add a compact API triage schematic and
         a deterministic link back to the exact scenario in Tests. */
      .exception-view .tag-test-status > td:last-child { min-width: 0 !important; }
      .exception-view .tag-test-status > td:last-child > a.linked {
        color: var(--atf-accent) !important;
        font-weight: 600 !important;
      }
      .exception-view .test-list-tools {
        min-height: 42px !important;
        clear: both !important;
      }
      .exception-view .test-list-tools > .tools {
        float: none !important;
        margin: 0 !important;
      }
      .exception-view .test-list-wrapper {
        clear: both !important;
        width: 100% !important;
      }
      .exception-view .test-list-wrapper > .test-list-item {
        width: 100% !important;
      }
      .atf-failure-schematic {
        display: grid !important;
        grid-template-columns: repeat(2, minmax(0, 1fr)) !important;
        gap: 6px !important;
        margin-top: 8px !important;
        padding: 8px !important;
        border: 1px solid var(--atf-border) !important;
        border-radius: 6px !important;
        background: var(--atf-panel-2) !important;
      }
      .atf-failure-field { min-width: 0 !important; display: flex !important; flex-direction: column !important; gap: 2px !important; }
      .atf-failure-field.atf-failure-cause { grid-column: 1 / -1 !important; }
      .atf-failure-label { color: var(--atf-muted) !important; font-size: .62rem !important; font-weight: 650 !important; letter-spacing: .06em !important; }
      .atf-failure-value { color: var(--atf-text) !important; font-size: .76rem !important; line-height: 1.3 !important; overflow-wrap: anywhere !important; }
      .atf-failure-open {
        grid-column: 1 / -1 !important;
        justify-self: start !important;
        padding: 4px 9px !important;
        border: 1px solid var(--atf-accent) !important;
        border-radius: 4px !important;
        background: transparent !important;
        color: var(--atf-accent) !important;
        font-size: .70rem !important;
        font-weight: 650 !important;
        cursor: pointer !important;
      }
      .atf-failure-open:hover { background: __ATF_ACCENT12__ !important; }
      .atf-failure-index {
        margin: 18px 20px !important;
        padding: 18px !important;
        border: 1px solid var(--atf-border) !important;
        border-radius: 8px !important;
        background: var(--atf-panel-2) !important;
      }
      .atf-failure-index-title {
        margin: 0 0 14px !important;
        color: var(--atf-text) !important;
        font-size: 1rem !important;
        font-weight: 650 !important;
      }
      .atf-failure-index-row {
        display: grid !important;
        grid-template-columns: 112px minmax(0, 1fr) !important;
        gap: 12px !important;
        padding: 8px 0 !important;
        border-top: 1px solid var(--atf-border) !important;
      }
      .atf-failure-index-cause { grid-template-columns: 112px minmax(0, 1fr) !important; }
      .atf-failure-index .atf-failure-open { margin-top: 12px !important; }
      .exception-view .test-list-item .test-item[status="fail"] { border-left: 3px solid var(--atf-bad) !important; }
      .exception-view .test-content { min-width: 0 !important; }
      @media (max-width: 900px) {
        .atf-failure-schematic { grid-template-columns: minmax(0, 1fr) !important; }
        .atf-failure-field.atf-failure-cause { grid-column: auto !important; }
        .atf-failure-open { grid-column: auto !important; }
        .atf-failure-index-row, .atf-failure-index-cause { grid-template-columns: minmax(0, 1fr) !important; gap: 3px !important; }
      }
    """;
}

