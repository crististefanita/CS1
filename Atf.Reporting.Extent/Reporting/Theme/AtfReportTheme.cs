namespace Atf.Reporting.Extent.Theme;

public static class AtfReportTheme
{
    // The CSS builder expands the central AtfReportColors palette into its token placeholders.
    private static readonly IReadOnlyDictionary<string, string> Tokens =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
        ["__ATF_JSONSURFACE__"] = AtfReportColors.JsonSurface,
        ["__ATF_ACCENTCONTRAST__"] = AtfReportColors.AccentContrast,
        ["__ATF_JSONCONTAINER__"] = AtfReportColors.JsonContainer,
        ["__ATF_DARK__"] = AtfReportColors.Dark,
        ["__ATF_BORDER__"] = AtfReportColors.Border,
        ["__ATF_REFERENCEBORDER__"] = AtfReportColors.ReferenceBorder,
        ["__ATF_GOOD__"] = AtfReportColors.Good,
        ["__ATF_TABLEBORDER__"] = AtfReportColors.TableBorder,
        ["__ATF_SETUP__"] = AtfReportColors.Setup,
        ["__ATF_RESPONSEGOOD__"] = AtfReportColors.ResponseGood,
        ["__ATF_METADATA__"] = AtfReportColors.Metadata,
        ["__ATF_ACCENT__"] = AtfReportColors.Accent,
        ["__ATF_OVERVIEWMUTED__"] = AtfReportColors.OverviewMuted,
        ["__ATF_MUTED__"] = AtfReportColors.Muted,
        ["__ATF_EXTENTPASS__"] = AtfReportColors.ExtentPass,
        ["__ATF_TABLELABEL__"] = AtfReportColors.TableLabel,
        ["__ATF_TABLETEXT__"] = AtfReportColors.TableText,
        ["__ATF_CODETEXT__"] = AtfReportColors.CodeText,
        ["__ATF_EXTENTFAIL__"] = AtfReportColors.ExtentFail,
        ["__ATF_TEXT__"] = AtfReportColors.Text,
        ["__ATF_JSONTEXT__"] = AtfReportColors.JsonText,
        ["__ATF_WARNING__"] = AtfReportColors.Warning,
        ["__ATF_DANGER__"] = AtfReportColors.Danger,
        ["__ATF_EXTENTSKIP__"] = AtfReportColors.ExtentSkip,
        ["__ATF_WHITE__"] = AtfReportColors.White,
        ["__ATF_SHADOWSOFT__"] = AtfReportColors.ShadowSoft,
        ["__ATF_SHADOWSTRONG__"] = AtfReportColors.ShadowStrong,
        ["__ATF_ACCENT045__"] = AtfReportColors.Accent045,
        ["__ATF_ACCENT055__"] = AtfReportColors.Accent055,
        ["__ATF_ACCENT07__"] = AtfReportColors.SecondarySectionHeadingBackground,
        ["__ATF_ACCENT08__"] = AtfReportColors.Accent08,
        ["__ATF_ACCENT12__"] = AtfReportColors.SecondarySectionHeadingHoverBackground,
        ["__ATF_ACCENT14__"] = AtfReportColors.Accent14,
        ["__ATF_ACCENT42__"] = AtfReportColors.SecondarySectionHeadingBorder,
        ["__ATF_ACCENT45__"] = AtfReportColors.Accent45,
        ["__ATF_GRAYBADGE__"] = AtfReportColors.GrayBadge,
        ["__ATF_WARNING10__"] = AtfReportColors.Warning10,
        ["__ATF_DANGER08__"] = AtfReportColors.Danger08,
        ["__ATF_DANGER10__"] = AtfReportColors.Danger10,
        ["__ATF_DANGER55__"] = AtfReportColors.Danger55,
        ["__ATF_WHITE018__"] = AtfReportColors.White018,
        ["__ATF_WHITE02__"] = AtfReportColors.White02,
        ["__ATF_WHITE025__"] = AtfReportColors.White025,
        ["__ATF_WHITE03__"] = AtfReportColors.White03,
        ["__ATF_WHITE055__"] = AtfReportColors.White055,
        ["__ATF_GOOD10__"] = AtfReportColors.Good10,
        ["__ATF_FONT_SANS__"] = AtfReportTypography.SansFontFamily,
        ["__ATF_FONT_CODE__"] = AtfReportTypography.CodeFontFamily,
        ["__ATF_FONT_BASE_SIZE__"] = AtfReportTypography.BaseFontSize,
        ["__ATF_FONT_OVERVIEW_SIZE__"] = AtfReportTypography.OverviewFontSize,
        ["__ATF_FONT_JSON_SIZE__"] = AtfReportTypography.JsonFontSize,
        ["__ATF_FONT_TABLE_HEADER_SIZE__"] = AtfReportTypography.TableHeaderFontSize,
        ["__ATF_FONT_TABLE_CELL_SIZE__"] = AtfReportTypography.TableCellFontSize,
        ["__ATF_FONT_BADGE_SIZE__"] = AtfReportTypography.BadgeFontSize,
        };

    public static string BuildCss()
    {
        var css = """
    <style id="atf-custom-theme">
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
      @media (max-width: 900px) {
        .atf-failure-schematic { grid-template-columns: minmax(0, 1fr) !important; }
        .atf-failure-field.atf-failure-cause { grid-column: auto !important; }
        .atf-failure-open { grid-column: auto !important; }
      }
    
      .category-container,
      .category-container .table,
      .category-container .table thead,
      .category-container .table tbody,
      .category-container .table tr,
      .category-container .table th,
      .category-container .table td {
        background: var(--atf-bg) !important;
      }
      .category-container { border: 1px solid var(--atf-border) !important; border-radius: 0 !important; box-shadow: 0 10px 26px __ATF_SHADOWSTRONG__ !important; }
      .dashboard-view .card,
      .dashboard-view .card-header,
      .dashboard-view .card-body,
      .dashboard-view .card-footer,
      .dashboard-view .panel,
      .dashboard-view .table,
      .dashboard-view .table thead,
      .dashboard-view .table tbody,
      .dashboard-view .table tr,
      .dashboard-view .table th,
      .dashboard-view .table td,
      .category-view .card,
      .category-view .card-header,
      .category-view .card-body,
      .category-view .card-footer {
        background: var(--atf-bg) !important;
      }
      .dashboard-view .category-container {
        border: 0 !important;
        padding-left: 15px !important;
        padding-right: 15px !important;
        box-shadow: none !important;
      }
      .dashboard-view .category-container,
      .dashboard-view .category-container > .card,
      .dashboard-view .category-container > .card > .card-header,
      .dashboard-view .category-container > .card > .card-body {
        border: 0 !important;
        outline: 0 !important;
        box-shadow: none !important;
      }
      .dashboard-view .category-container > .card > .card-header {
        padding: 0 10px 4px !important;
        min-height: 0 !important;
        height: auto !important;
      }
      .dashboard-view .category-container > .card > .card-header p {
        margin: 0 !important;
        line-height: 1.35 !important;
      }
      .dashboard-view .category-container > .card > .card-body,
      .dashboard-view .sysenv-container > .card > .card-body {
        padding: 0 !important;
        height: 500px !important;
        max-height: 500px !important;
        overflow-x: hidden !important;
        overflow-y: auto !important;
      }
      .dashboard-view .category-container > .card > .card-body > table {
        margin: 0 !important;
        border: 1px solid __ATF_TABLEBORDER__ !important;
        border-collapse: collapse !important;
      }
      .dashboard-view .sysenv-container,
      .dashboard-view .sysenv-container > .card,
      .dashboard-view .sysenv-container > .card > .card-header,
      .dashboard-view .sysenv-container > .card > .card-body {
        border: 0 !important;
        outline: 0 !important;
        box-shadow: none !important;
      }
      .dashboard-view .sysenv-container > .card > .card-header {
        padding: 0 10px 4px !important;
        min-height: 0 !important;
        height: auto !important;
      }
      .dashboard-view .sysenv-container > .card > .card-header p {
        margin: 0 !important;
        line-height: 1.35 !important;
      }
      .dashboard-view .sysenv-container > .card > .card-body { padding: 0 !important; }
      .dashboard-view .sysenv-container > .card > .card-body > table {
        margin: 0 !important;
        border: 1px solid __ATF_TABLEBORDER__ !important;
        border-collapse: collapse !important;
      }
      .dashboard-view .sysenv-container .table th,
      .dashboard-view .sysenv-container .table td { overflow-wrap: anywhere !important; word-break: break-word !important; }
    
      /* Keep the dashboard tables aligned with the reference Extent layout:
         two equal columns, each with one card frame and one table frame. */
      @media (min-width: 768px) {
        .dashboard-view .category-container,
        .dashboard-view .sysenv-container {
          flex: 0 0 50% !important;
          max-width: 50% !important;
        }
      }
      @media (max-width: 767px) {
        .dashboard-view .category-container,
        .dashboard-view .sysenv-container {
          flex: 0 0 100% !important;
          max-width: 100% !important;
        }
      }
      .dashboard-view .category-container > .card,
      .dashboard-view .sysenv-container > .card {
        height: 500px !important;
        margin-bottom: 0 !important;
        border: 1px solid var(--atf-border) !important;
        border-radius: 0 !important;
        background: var(--atf-bg) !important;
        box-shadow: none !important;
        overflow: hidden !important;
      }
      .dashboard-view .category-container > .card > .card-header,
      .dashboard-view .sysenv-container > .card > .card-header {
        height: auto !important;
        min-height: 0 !important;
        padding: 14px 24px 16px !important;
        background: var(--atf-bg) !important;
        border: 0 !important;
      }
      .dashboard-view .category-container > .card > .card-header p,
      .dashboard-view .sysenv-container > .card > .card-header p {
        margin: 0 !important;
        line-height: 1.35 !important;
      }
      /* Extent's Overview cards retain Bootstrap's light-theme text rules on
         labels and values even after their surfaces become dark. Normalize
         every human-readable Overview label/value to the ATF text token. */
      .dashboard-view .card > .card-header p,
      .dashboard-view .card > .card-body p,
      .dashboard-view .card > .card-body h3,
      .dashboard-view .card > .card-body h6,
      .dashboard-view .card > .card-footer,
      .dashboard-view .card > .card-footer *,
      .dashboard-view .card h6 {
        color: var(--atf-text) !important;
        font-family: var(--atf-font-sans) !important;
      }

      /* Native Extent Overview cards use a light-theme border by default.
         Keep the dark reference's restrained one-pixel frame for the four
         summary cards, charts and timeline without affecting the Tags and
         System/Environment table frames below. */
      .dashboard-view .row > .col-md-3 > .card,
      .dashboard-view .row > .col-md-12 > .card,
      .dashboard-view .row > .col-md-3 > .card > .card-header,
      .dashboard-view .row > .col-md-3 > .card > .card-body,
      .dashboard-view .row > .col-md-3 > .card > .card-footer,
      .dashboard-view .row > .col-md-12 > .card > .card-header,
      .dashboard-view .row > .col-md-12 > .card > .card-body,
      .dashboard-view .row > .col-md-12 > .card > .card-footer {
        border-color: var(--atf-reference-border) !important;
      }
      .dashboard-view .category-container > .card > .card-body,
      .dashboard-view .sysenv-container > .card > .card-body {
        height: calc(100% - 54px) !important;
        max-height: calc(100% - 54px) !important;
        padding: 0 24px 16px !important;
        background: var(--atf-bg) !important;
        overflow-x: hidden !important;
        overflow-y: auto !important;
      }
      .dashboard-view .category-container > .card > .card-body > table,
      .dashboard-view .sysenv-container > .card > .card-body > table {
        width: 100% !important;
        margin: 0 !important;
        border: 1px solid __ATF_TABLEBORDER__ !important;
        border-collapse: collapse !important;
      }
      /* Tags and System/Environment are dashboard-level secondary
         sections. Reuse the exact heading tokens used by the custom ATF
         metadata disclosures, without changing their table surfaces. */
      .dashboard-view .category-container > .card > .card-header,
      .dashboard-view .sysenv-container > .card > .card-header {
        color: var(--atf-secondary-heading-text) !important;
        background: var(--atf-secondary-heading-bg) !important;
        border-bottom: 1px solid var(--atf-border) !important;
      }
      .dashboard-view .category-container > .card > .card-header p,
      .dashboard-view .sysenv-container > .card > .card-header p {
        color: var(--atf-secondary-heading-text) !important;
      }
      /* Keep Tags/System-Environment headings and column headers native;
         only the data cells use the same 90% overview support scale. */
      .dashboard-view .category-container > .card > .card-body > table tbody td,
      .dashboard-view .category-container > .card > .card-body > table tbody th,
      .dashboard-view .sysenv-container > .card > .card-body > table tbody td,
      .dashboard-view .sysenv-container > .card > .card-body > table tbody th {
        font-family: var(--atf-overview-font-family) !important;
        font-size: var(--atf-overview-font-size) !important;
        line-height: 1.45 !important;
      }
    
      /* Keep the secondary disclosure bars on the exact same content rail as
         the native dashboard zones. The generated Extent row/column carries
         Bootstrap gutters by default; those gutters were the source of the
         visibly shorter bars. */
      .atf-secondary-row {
        width: 100% !important;
        margin: .25rem 0 0 !important;
        padding-left: 0 !important;
        padding-right: 0 !important;
      }
      .atf-secondary-row > .col-md-12 {
        width: 100% !important;
        max-width: 100% !important;
        flex: 0 0 100% !important;
        margin: 0 !important;
        padding-left: 0 !important;
        padding-right: 0 !important;
      }
      .atf-secondary-overview {
        margin: .35rem 0 0 !important;
        color: var(--atf-text) !important;
        background: var(--atf-panel) !important;
        border: 1px solid var(--atf-border) !important;
        border-radius: 8px !important;
        overflow: hidden !important;
      }
      .atf-secondary-overview > summary {
        display: flex !important;
        align-items: center !important;
        gap: 10px !important;
        min-width: 0 !important;
        padding: 10px 12px !important;
        color: var(--atf-secondary-heading-text) !important;
        background: var(--atf-secondary-heading-bg) !important;
        cursor: pointer !important;
        list-style: none !important;
        user-select: none !important;
      }
      .atf-secondary-overview > summary::-webkit-details-marker,
      .atf-secondary-overview > summary::marker { display: none !important; content: '' !important; }
      .atf-secondary-overview > summary::before { content: '[+] expand' !important; flex: 0 0 auto !important; padding: 2px 8px !important; color: var(--atf-secondary-heading-text) !important; border: 1px solid var(--atf-secondary-heading-border) !important; border-radius: 999px !important; font-size: .68rem !important; font-weight: 600 !important; }
      .atf-secondary-overview[open] > summary::before { content: '[-] collapse' !important; color: var(--atf-muted) !important; border-color: var(--atf-border) !important; }
      .atf-secondary-overview > summary:hover { background: var(--atf-secondary-heading-bg-hover) !important; }
      /* Match native Tags/System-Environment card headings. The previous
         .82rem/.68rem pair made the secondary bars look like metadata
         footnotes instead of first-class Overview sections. */
      .atf-secondary-overview-title { min-width: 0 !important; overflow-wrap: anywhere !important; font-size: 1.1rem !important; line-height: 1.4 !important; font-weight: 550 !important; }
      .atf-secondary-overview-badge { margin-left: auto !important; flex: 0 0 auto !important; color: __ATF_OVERVIEWMUTED__ !important; font-size: var(--atf-overview-font-size) !important; line-height: 1.35 !important; font-weight: 400 !important; }
      .atf-secondary-body { max-height: 230px !important; overflow-x: hidden !important; overflow-y: auto !important; padding: 12px 14px !important; background: var(--atf-bg) !important; }
      .atf-secondary-note { margin: 0 0 10px !important; color: var(--atf-muted) !important; font-size: var(--atf-overview-font-size) !important; line-height: 1.45 !important; }
      .atf-secondary-grid { display: grid !important; grid-template-columns: repeat(4, minmax(0, 1fr)) !important; gap: 8px !important; }
      .atf-secondary-metric { min-width: 0 !important; padding: 8px 10px !important; background: __ATF_WHITE02__ !important; border: 1px solid var(--atf-border) !important; }
      .atf-secondary-metric-label { color: var(--atf-muted) !important; font-size: var(--atf-overview-font-size) !important; text-transform: uppercase !important; letter-spacing: .05em !important; }
      .atf-secondary-metric-value { margin-top: 3px !important; color: var(--atf-text) !important; font-size: var(--atf-overview-font-size) !important; font-weight: 500 !important; }
      .atf-secondary-reading-rule { margin: 10px 0 0 !important; color: var(--atf-muted) !important; font-size: var(--atf-overview-font-size) !important; line-height: 1.45 !important; }
      @media (max-width: 1100px) { .atf-secondary-grid { grid-template-columns: repeat(2, minmax(0, 1fr)) !important; } }
      @media (max-width: 600px) { .atf-secondary-grid { grid-template-columns: 1fr !important; } }
      /* Reference-compatible three-column secondary audit. Each card is a
         single dark frame; only its table body scrolls vertically. */
      .atf-tag-audit-disclosure {
        border: 0 !important;
        border-radius: 0 !important;
        background: transparent !important;
        overflow: visible !important;
      }
      .atf-tag-audit-disclosure > .atf-tag-audit-body {
        padding: 0 !important;
        background: transparent !important;
      }
      .atf-tag-audit-body { width: 100% !important; padding: 12px 0 14px !important; background: var(--atf-bg) !important; }
      .atf-tag-grid { display: grid !important; grid-template-columns: repeat(3, minmax(0, 1fr)) !important; gap: 12px !important; align-items: start !important; width: 100% !important; }
      .atf-tag-card { min-width: 0 !important; border: 0 !important; border-radius: 0 !important; background: transparent !important; box-shadow: none !important; overflow: visible !important; }
      .dashboard-view .atf-tag-card { min-width: 0 !important; border: 0 !important; border-radius: 0 !important; background: transparent !important; box-shadow: none !important; overflow: visible !important; }
      /* Audit card headings are first-class Overview headings. Keep their
         typography and accent colour identical to native Tags/System-Environment
         headings; only the table body remains secondary/compact. */
      .atf-tag-card .card-header { min-height: 0 !important; padding: 14px 24px 16px !important; border: 1px solid var(--atf-border) !important; border-bottom: 0 !important; border-radius: 8px 8px 0 0 !important; background: var(--atf-secondary-heading-bg) !important; color: var(--atf-secondary-heading-text) !important; font-family: var(--atf-overview-font-family) !important; font-size: var(--atf-font-base-size) !important; line-height: 1.35 !important; }
      .dashboard-view .atf-tag-card > .card-header p { margin: 0 !important; color: var(--atf-secondary-heading-text) !important; font-family: var(--atf-overview-font-family) !important; font-size: var(--atf-font-base-size) !important; font-weight: 550 !important; line-height: 1.35 !important; }
      .atf-tag-card .card-body { padding: 0 !important; background: transparent !important; }
      .atf-tag-scroll { height: var(--atf-audit-scroll-height) !important; max-height: var(--atf-audit-scroll-height) !important; overflow-x: hidden !important; overflow-y: auto !important; border: 1px solid var(--atf-border) !important; border-top: 0 !important; border-radius: 0 0 8px 8px !important; background: var(--atf-bg) !important; }
      .atf-tag-table { width: 100% !important; margin: 0 !important; table-layout: fixed !important; border: 0 !important; border-collapse: collapse !important; font-family: var(--atf-overview-font-family) !important; font-size: var(--atf-overview-font-size) !important; line-height: 1.32 !important; }
      .atf-tag-table th, .atf-tag-table td { padding: 7px 9px !important; border: 1px solid var(--atf-border) !important; color: var(--atf-text) !important; background: var(--atf-bg) !important; overflow-wrap: anywhere !important; word-break: break-word !important; vertical-align: top !important; font-family: var(--atf-overview-font-family) !important; font-size: var(--atf-overview-font-size) !important; }
      .atf-tag-table th { color: var(--atf-muted) !important; background: var(--atf-panel-2) !important; font-weight: 600 !important; letter-spacing: .05em !important; text-transform: uppercase !important; text-align: left !important; }
      .atf-tag-table th:nth-child(n+2), .atf-tag-table td:nth-child(n+2) { width: 4.2rem !important; text-align: right !important; white-space: nowrap !important; }
      .atf-tag-table thead th { position: sticky !important; top: 0 !important; z-index: 1 !important; }
      .atf-tag-table tbody tr:nth-child(odd) td { background: __ATF_WHITE018__ !important; }
      .atf-tag-table tbody tr:hover td { background: __ATF_ACCENT055__ !important; }
      @media (max-width: 1100px) { .atf-tag-grid { grid-template-columns: 1fr !important; } }
      .dashboard-view .category-container > .card {
        border: 0 !important;
        margin-bottom: 0 !important;
        box-shadow: none !important;
      }
      .dashboard-view .category-container > .card > .card-header {
        padding-left: 10px !important;
        padding-right: 10px !important;
      }
      /* Final precedence layer: the legacy Extent category override above
         removes the card border. Restore the single reference-style frame
         after that rule, without reintroducing any nested wrapper border. */
      .dashboard-view .category-container > .card {
        border: 1px solid var(--atf-border) !important;
        background: var(--atf-bg) !important;
      }
      .dashboard-view .category-container > .card > .card-header {
        padding-left: 24px !important;
        padding-right: 24px !important;
      }
      /* Native Tags and System/Environment use the same single-panel
         contract as the audit cards: framed heading, connected scroll body,
         and one border system for every table cell. */
      .dashboard-view .category-container > .card,
      .dashboard-view .sysenv-container > .card {
        min-width: 0 !important;
        border: 0 !important;
        border-radius: 0 !important;
        background: transparent !important;
        box-shadow: none !important;
        overflow: visible !important;
      }
      .dashboard-view .category-container > .card > .card-header,
      .dashboard-view .sysenv-container > .card > .card-header {
        height: auto !important;
        min-height: 0 !important;
        padding: 14px 24px 16px !important;
        border: 1px solid var(--atf-border) !important;
        border-bottom: 0 !important;
        border-radius: 8px 8px 0 0 !important;
        background: var(--atf-secondary-heading-bg) !important;
        color: var(--atf-secondary-heading-text) !important;
        font-family: var(--atf-overview-font-family) !important;
        font-size: var(--atf-font-base-size) !important;
        line-height: 1.35 !important;
      }
      .dashboard-view .category-container > .card > .card-body,
      .dashboard-view .sysenv-container > .card > .card-body {
        height: calc(100% - 54px) !important;
        max-height: calc(100% - 54px) !important;
        padding: 0 !important;
        border: 1px solid var(--atf-border) !important;
        border-top: 0 !important;
        border-radius: 0 0 8px 8px !important;
        background: var(--atf-bg) !important;
        overflow-x: hidden !important;
        overflow-y: auto !important;
      }
      .dashboard-view .category-container > .card > .card-body > table,
      .dashboard-view .sysenv-container > .card > .card-body > table {
        width: 100% !important;
        margin: 0 !important;
        table-layout: fixed !important;
        border: 0 !important;
        border-collapse: collapse !important;
        font-family: var(--atf-overview-font-family) !important;
        font-size: var(--atf-overview-font-size) !important;
        line-height: 1.32 !important;
      }
      .dashboard-view .category-container > .card > .card-body > table th,
      .dashboard-view .category-container > .card > .card-body > table td,
      .dashboard-view .sysenv-container > .card > .card-body > table th,
      .dashboard-view .sysenv-container > .card > .card-body > table td {
        padding: 7px 9px !important;
        border: 1px solid var(--atf-border) !important;
        color: var(--atf-text) !important;
        background: var(--atf-bg) !important;
        overflow-wrap: anywhere !important;
        word-break: break-word !important;
        vertical-align: top !important;
        font-family: var(--atf-overview-font-family) !important;
        font-size: var(--atf-overview-font-size) !important;
        line-height: 1.32 !important;
      }
      .dashboard-view .category-container > .card > .card-body > table th,
      .dashboard-view .sysenv-container > .card > .card-body > table th {
        color: var(--atf-muted) !important;
        background: var(--atf-panel-2) !important;
        font-weight: 600 !important;
        letter-spacing: .05em !important;
        text-transform: uppercase !important;
        text-align: left !important;
      }
      .dashboard-view .category-container > .card > .card-body > table thead th,
      .dashboard-view .sysenv-container > .card > .card-body > table thead th {
        position: sticky !important;
        top: 0 !important;
        z-index: 1 !important;
      }
      .dashboard-view .category-container > .card > .card-body > table tbody tr:nth-child(odd) td,
      .dashboard-view .sysenv-container > .card > .card-body > table tbody tr:nth-child(odd) td {
        background: __ATF_WHITE018__ !important;
      }
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
    
      details.step-detail {
        margin: 8px 0 !important;
        color: var(--atf-text) !important;
        background: var(--atf-panel-2) !important;
        border: 1px solid var(--atf-border) !important;
        border-radius: 10px !important;
        overflow: hidden !important;
      }
      details.step-detail > summary {
        display: flex !important;
        align-items: center !important;
        gap: 8px !important;
        padding: 8px 12px !important;
        cursor: pointer !important;
        color: var(--atf-accent) !important;
        background: var(--atf-bg) !important;
        list-style: none !important;
        user-select: none !important;
        transition: background .15s ease !important;
      }
      details.step-detail > summary:hover { background: __ATF_ACCENT08__ !important; }
      details.step-detail > summary::-webkit-details-marker,
      details.step-detail > summary::marker { display: none !important; content: '' !important; }
      .se-toggle {
        display: inline-block !important;
        padding: 1px 9px !important;
        color: var(--atf-accent) !important;
        background: __ATF_ACCENT14__ !important;
        border: 1px solid __ATF_ACCENT45__ !important;
        border-radius: 999px !important;
        font-size: 11px !important;
        font-weight: 600 !important;
        white-space: nowrap !important;
      }
      .se-close { display: none !important; }
      details.step-detail[open] > summary .se-open { display: none !important; }
      details.step-detail[open] > summary .se-close { display: inline-block !important; color: var(--atf-muted) !important; background: transparent !important; border-color: var(--atf-border) !important; }
      .se-title { flex: 1 1 auto !important; min-width: 0 !important; max-width: 100% !important; overflow: hidden !important; text-overflow: ellipsis !important; white-space: nowrap !important; font-weight: 500 !important; }
      details.step-detail .body { padding: 10px 14px !important; color: var(--atf-text) !important; }
      details.step-detail .body > :first-child { margin-top: 0 !important; }
      details.step-detail .body > :last-child { margin-bottom: 0 !important; }
      .kv { color: var(--atf-muted) !important; margin: 2px 0 !important; }
      .kv b { color: var(--atf-text) !important; font-weight: 600 !important; }
      .helm-meta-grid { display: grid !important; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)) !important; gap: 8px !important; margin: 2px 0 !important; }
      .helm-meta-item { min-width: 0 !important; padding: 8px 10px !important; background: __ATF_WHITE02__ !important; border: 1px solid var(--atf-border) !important; border-radius: 8px !important; }
      .helm-meta-label { margin-bottom: 4px !important; color: var(--atf-muted) !important; font-size: .72rem !important; text-transform: uppercase !important; letter-spacing: .05em !important; overflow-wrap: anywhere !important; }
      .helm-meta-value { color: var(--atf-text) !important; overflow-wrap: anywhere !important; word-break: break-word !important; }
       .helm-evidence-table-wrap { width: 100% !important; max-width: 100% !important; overflow-x: hidden !important; }
       .helm-trace-table-wrap { max-height: 280px !important; overflow-x: hidden !important; overflow-y: auto !important; scrollbar-gutter: stable !important; }
      .helm-evidence-table { box-sizing: border-box !important; width: 100% !important; max-width: 100% !important; table-layout: fixed !important; border-collapse: collapse !important; margin: 0 !important; }
      .helm-evidence-table th, .helm-evidence-table td { box-sizing: border-box !important; min-width: 0 !important; padding: 7px 9px !important; vertical-align: top !important; border: 1px solid var(--atf-border) !important; overflow-wrap: anywhere !important; word-break: break-word !important; }
      .helm-evidence-table th { width: 28% !important; color: var(--atf-muted) !important; background: __ATF_WHITE025__ !important; font-size: .70rem !important; font-weight: 550 !important; text-transform: uppercase !important; letter-spacing: .06em !important; text-align: left !important; }
      .helm-evidence-table th[scope="row"] { color: __ATF_TABLELABEL__ !important; font-size: .78rem !important; font-weight: 450 !important; text-transform: none !important; letter-spacing: 0 !important; }
      .helm-evidence-table td { color: __ATF_TABLETEXT__ !important; background: var(--atf-bg) !important; font-weight: 400 !important; }
       /* Technical Trace: let FIELD size itself from the longest field name,
          while VALUE owns the remaining width and wraps like JSON evidence. */
       .helm-trace-table { table-layout: auto !important; }
       .helm-trace-table thead th:first-child,
       .helm-trace-table tbody th[scope="row"] {
         width: 1% !important;
         min-width: 0 !important;
         white-space: nowrap !important;
         overflow-wrap: normal !important;
         word-break: normal !important;
         color: __ATF_TABLELABEL__ !important;
         font-weight: 500 !important;
       }
       .helm-trace-table thead th:last-child,
       .helm-trace-table tbody td {
         width: auto !important;
         min-width: 0 !important;
         max-width: 0 !important;
         color: __ATF_CODETEXT__ !important;
         white-space: normal !important;
         overflow-wrap: anywhere !important;
         word-break: break-word !important;
       }
      .helm-assertion-table th:nth-child(1) { width: 34% !important; }
      .helm-assertion-table th:nth-child(2) { width: 27% !important; }
      .helm-assertion-table th:nth-child(3) { width: 39% !important; }
      .helm-validation-table th:nth-child(1) { width: 31% !important; }
      .helm-validation-table th:nth-child(2) { width: 23% !important; }
      .helm-validation-table th:nth-child(3) { width: 34% !important; }
      .helm-validation-table th:nth-child(4), .helm-validation-table td:nth-child(4) { width: 12% !important; white-space: nowrap !important; text-align: center !important; }
      .assertion-result { display: inline-flex !important; align-items: center !important; justify-content: center !important; min-width: 1.4rem !important; font-family: "Segoe UI Emoji", "Apple Color Emoji", sans-serif !important; font-size: 1.05rem !important; line-height: 1 !important; }
      .helm-validation-table tr.assertion-fail td { background: __ATF_DANGER08__ !important; }
      .helm-api-classification { display: flex !important; align-items: center !important; gap: 10px !important; margin: 0 0 8px !important; padding: 1px 0 8px !important; border: 0 !important; opacity: .92 !important; }
      .helm-api-classification::after { content: "execution metadata" !important; color: __ATF_METADATA__ !important; font-size: .66rem !important; font-weight: 400 !important; letter-spacing: .02em !important; }
      .helm-api-classification-label { display: inline-flex !important; align-items: center !important; padding: 2px 7px !important; color: var(--atf-muted) !important; background: __ATF_GRAYBADGE__ !important; border: 0 !important; border-radius: 999px !important; font-size: .68rem !important; font-weight: 700 !important; letter-spacing: .05em !important; text-transform: uppercase !important; }
      .helm-api-classification + .helm-evidence-table-wrap { margin-left: 0 !important; width: 100% !important; max-width: 100% !important; padding-left: 0 !important; border-left: 0 !important; }
      details.step-detail.request-details .helm-api-classification { border-left: 0 !important; }
      details.step-detail.request-details .helm-api-classification-label { color: var(--atf-accent) !important; background: __ATF_ACCENT12__ !important; }
      details.step-detail.response-details .helm-api-classification { border-left: 0 !important; }
      details.step-detail.response-details .helm-api-classification-label { color: __ATF_RESPONSEGOOD__ !important; background: __ATF_GOOD10__ !important; }
      .helm-evidence-table tbody tr:hover th,
      .helm-evidence-table tbody tr:hover td { background: __ATF_ACCENT055__ !important; }
      /* JSON evidence block: keep this visually identical to the reference
         RegressionExtent renderer, including the dark surface, typography,
         spacing and scroll ownership. */
      .helm-scroll-block { margin: 8px 0 10px !important; background: __ATF_JSONCONTAINER__ !important; border: 1px solid var(--atf-border) !important; border-radius: 10px !important; overflow: hidden !important; }
      .helm-scroll-block-title { padding: 9px 12px !important; color: var(--atf-muted) !important; background: __ATF_WHITE03__ !important; border-bottom: 1px solid var(--atf-border) !important; font-size: .76rem !important; text-transform: uppercase !important; letter-spacing: .05em !important; }
      .helm-scroll-block-body { max-height: var(--atf-audit-scroll-height) !important; max-width: 100% !important; min-width: 0 !important; overflow-x: hidden !important; overflow-y: auto !important; scrollbar-gutter: stable !important; }
      .helm-scroll-block-body pre { box-sizing: border-box !important; min-height: 0 !important; min-width: 0 !important; max-width: 100% !important; max-height: none !important; margin: 0 !important; padding: 10px 12px !important; color: var(--atf-json-text) !important; background: var(--atf-json-surface) !important; border: 0 !important; border-radius: 0 !important; font-family: var(--atf-font-code) !important; font-size: var(--atf-json-font-size) !important; font-weight: 400 !important; line-height: 1.5 !important; letter-spacing: normal !important; white-space: pre-wrap !important; overflow: visible !important; overflow-wrap: normal !important; word-break: break-word !important; }
      details.step-detail pre { margin: 6px 0 0 !important; padding: 10px 12px !important; color: var(--atf-json-text) !important; background: var(--atf-json-surface) !important; border: 1px solid var(--atf-border) !important; border-radius: 8px !important; font-family: var(--atf-font-code) !important; font-size: var(--atf-json-font-size) !important; font-weight: 400 !important; line-height: 1.5 !important; letter-spacing: normal !important; white-space: pre-wrap !important; overflow: auto !important; word-break: break-word !important; overflow-wrap: normal !important; }
      details.step-detail .helm-scroll-block-body { max-height: var(--atf-audit-scroll-height) !important; max-width: 100% !important; min-width: 0 !important; overflow-x: hidden !important; overflow-y: auto !important; }
      details.step-detail .helm-scroll-block-body pre { box-sizing: border-box !important; min-height: 0 !important; min-width: 0 !important; max-width: 100% !important; max-height: none !important; margin: 0 !important; padding: 10px 12px !important; color: var(--atf-json-text) !important; background: var(--atf-json-surface) !important; border: 0 !important; border-radius: 0 !important; font-family: var(--atf-font-code) !important; font-size: var(--atf-json-font-size) !important; font-weight: 400 !important; line-height: 1.5 !important; letter-spacing: normal !important; white-space: pre-wrap !important; overflow: visible !important; overflow-wrap: normal !important; word-break: break-word !important; }
      details.step-detail.fail { border-color: __ATF_DANGER55__ !important; }
      details.step-detail.fail > summary { color: var(--atf-bad) !important; background: __ATF_DANGER10__ !important; }
      details.step-detail.skip > summary, details.step-detail.warn > summary { color: var(--atf-warn) !important; background: __ATF_WARNING10__ !important; }
      pre, code { color: __ATF_CODETEXT__ !important; }
    
      /* Reference-compatible typography for API messages and evidence
         tables. JSON remains the deliberate monospace exception above. */
      html, body, body.dark,
      .app, .layout, .main-content, .content,
      .header.navbar, .side-nav, .side-nav-inner,
      .test-list, .test-content, .detail-body,
      .dashboard-view, .category-view,
      .test-list *, .test-content *, .dashboard-view *, .category-view * {
        font-family: var(--atf-font-sans) !important;
        font-size: var(--atf-font-base-size);
        line-height: 1.55;
        font-weight: 400 !important;
        letter-spacing: 0;
      }
      .card-title, .card-title span, .card-title a,
      .card-header, .card-header p, .card-header h6,
      .test-name, .test-list .name,
      .dashboard-view h3, .dashboard-view h6 {
        font-weight: 400 !important;
      }
      .test-list .test-detail .name {
        font-size: 1rem !important;
        line-height: 1.34 !important;
        font-weight: 400 !important;
        letter-spacing: 0 !important;
      }
      .test-list .test-detail .text-sm,
      .test-list .test-detail .text-sm span {
        font-size: .72rem !important;
        line-height: 1.25 !important;
      }
      .test-content h5.test-status {
        font-size: 1.10rem !important;
        line-height: 1.34 !important;
        font-weight: 400 !important;
        letter-spacing: 0 !important;
      }
      .test-contents .card-header .card-title,
      .test-contents .card-header .card-title a.node,
      .test-contents .card-header .card-title a.node span,
      details.step-detail > summary .se-title {
        font-size: 1rem !important;
        line-height: 1.34 !important;
        font-weight: 400 !important;
        letter-spacing: 0 !important;
      }
       /* The report typography reset also reaches Extent's icon elements.
          Restore the embedded Font Awesome face so tag/navigation glyphs
          render as icons rather than tofu squares. */
       .fa,
       .fa::before,
       [class^="fa-"],
       [class*=" fa-"]::before {
         font-family: "FontAwesome" !important;
         font-style: normal !important;
         font-weight: normal !important;
       }
      .test-view .test-contents .badge,
      .test-view .test-contents .log,
      .test-view .test-contents li .badge,
      .test-view .test-contents table .badge,
      .test-content .badge.badge-success,
      .test-content .badge.badge-danger,
      .test-content .badge.badge-default,
      .test-content .uri-anchor {
        font-size: var(--atf-badge-font-size) !important;
        line-height: 1.12 !important;
        font-weight: 400 !important;
      }
      details.step-detail .body {
        font-size: .9rem !important;
        line-height: 1.42 !important;
        font-weight: 400 !important;
        letter-spacing: 0 !important;
      }
      details.step-detail .body .kv,
      details.step-detail .body .kv b {
        font-size: .9rem !important;
        line-height: 1.42 !important;
        letter-spacing: 0 !important;
      }
      details.step-detail .body .kv b { font-weight: 500 !important; }
      details.step-detail .helm-api-classification-label {
        font-size: .68rem !important;
        line-height: 1.12 !important;
        font-weight: 400 !important;
      }
      .test-view table th,
      .helm-evidence-table th,
      .helm-assertion-table th,
      .helm-validation-table th {
        font-family: var(--atf-font-sans) !important;
        font-size: var(--atf-table-header-font-size) !important;
        line-height: 1.28 !important;
        font-weight: 400 !important;
        letter-spacing: 0 !important;
      }
      .test-view table td,
      .test-view table td span,
      .test-view table td div,
      .test-view table td p,
      .helm-evidence-table td,
      .helm-evidence-table td span,
      .helm-evidence-table td div,
      .helm-evidence-table td p,
      .helm-assertion-table td,
      .helm-validation-table td {
        font-family: var(--atf-font-sans) !important;
        font-size: var(--atf-table-cell-font-size) !important;
        line-height: 1.34 !important;
        font-weight: 400 !important;
        letter-spacing: 0 !important;
      }
      a { color: var(--atf-accent) !important; }
      .dropdown-menu { background: var(--atf-panel) !important; border: 1px solid var(--atf-border) !important; }
      .dropdown-item { color: var(--atf-text) !important; }
      .dropdown-item:hover { background: __ATF_ACCENT12__ !important; }
      .form-control { color: var(--atf-text) !important; background: var(--atf-panel-2) !important; border-color: var(--atf-border) !important; }
    
      /* Compact API execution nodes: the title and status badges need a
         breathable delta, not a large empty vertical shell. Keep this
         scoped to phase cards and their direct API children so dashboard
         cards and evidence tables retain their reference spacing. */
      .test-view .test-content-detail .card[data-atf-phase] {
        margin-bottom: 8px !important;
      }
      .test-view .test-content-detail .card[data-atf-phase] > .card-header {
        min-height: 0 !important;
        height: auto !important;
        padding: 5px 14px !important;
        line-height: 1.2 !important;
      }
      .test-view .test-content-detail .card[data-atf-phase] > .card-header .card-title,
      .test-view .test-content-detail .card[data-atf-phase] > .card-header .card-title a,
      .test-view .test-content-detail .card[data-atf-phase] > .card-header .card-title span {
        line-height: 1.22 !important;
        margin-top: 0 !important;
        margin-bottom: 0 !important;
      }
      .test-view .test-content-detail .card[data-atf-phase] > .accordion {
        margin: 6px 12px 8px !important;
      }
      .test-view .test-content-detail .card[data-atf-phase] > .accordion > .card {
        margin: 4px 0 !important;
      }
      .test-view .test-content-detail .card[data-atf-phase] > .accordion > .card > .card-header {
        min-height: 0 !important;
        height: auto !important;
        padding: 5px 12px !important;
        line-height: 1.2 !important;
      }
      .test-view .test-content-detail .card[data-atf-phase] > .accordion > .card > .card-header .card-title,
      .test-view .test-content-detail .card[data-atf-phase] > .accordion > .card > .card-header .card-title a,
      .test-view .test-content-detail .card[data-atf-phase] > .accordion > .card > .card-header .card-title span {
        line-height: 1.22 !important;
        margin-top: 0 !important;
        margin-bottom: 0 !important;
      }

      __ATF_EXECUTION_HIERARCHY_CSS__
      @media (max-width: 900px) {
        .view.d-none { display: none !important; }
        .test-wrapper { position: static !important; left: auto !important; right: auto !important; width: 100% !important; display: block !important; min-width: 0 !important; }
        .test-content { position: static !important; height: auto !important; min-height: 0 !important; display: block !important; }
        .test-list { width: 100% !important; max-height: 34vh !important; overflow-y: auto !important; border-right: 0 !important; }
        .test-contents { width: 100% !important; min-width: 0 !important; }
        .test-content-detail { position: static !important; width: 100% !important; height: auto !important; min-width: 0 !important; max-height: none !important; }
        .test-content-detail .detail-body,
        .test-contents > .mt-4,
        .test-contents > .mt-4 > .accordion { min-width: 0 !important; max-width: 100% !important; }
        details.step-detail > summary { min-width: 0 !important; }
        details.step-detail > summary .se-title { min-width: 0 !important; overflow-wrap: anywhere !important; word-break: break-word !important; }
      }
      @media (max-width: 600px) {
        .test-content-detail .detail-body > table { display: block !important; width: 100% !important; }
        .test-content-detail .detail-body > table > thead { display: none !important; }
        .test-content-detail .detail-body > table > tbody,
        .test-content-detail .detail-body > table > tbody > tr { display: block !important; width: 100% !important; }
        .test-content-detail .detail-body > table > tbody > tr { margin: 8px 0 !important; border: 1px solid var(--atf-border) !important; }
        .test-content-detail .detail-body > table > tbody > tr > td { display: block !important; width: 100% !important; padding: 5px 8px !important; border: 0 !important; }
        .test-content-detail .detail-body > table > tbody > tr > td:nth-child(1) { display: inline-block !important; width: auto !important; margin-right: 10px !important; vertical-align: top !important; }
        .test-content-detail .detail-body > table > tbody > tr > td:nth-child(3) { width: 100% !important; }
      }
    </style>
    """;

        foreach (var token in Tokens)
        {
            css = css.Replace(token.Key, token.Value, StringComparison.Ordinal);
        }

        return css.Replace(
            "__ATF_EXECUTION_HIERARCHY_CSS__",
            AtfReportThemeFragments.BuildExecutionHierarchyCss(),
            StringComparison.Ordinal);
    }
}





