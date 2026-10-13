namespace Atf.Reporting.Extent.Theme.Css;

/// <summary>Overview surface, category and native summary card rules.</summary>
internal static class AtfReportThemeOverviewBaseFragments
{
    public static string Build() => """
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
      /* Category mixes Extent's native pass badge with ATF's skip/fail
         variants. Normalize the shared status silhouette so every semantic
         label occupies exactly the same vertical space. */
      .category-view .badge.log {
        display: inline-flex !important;
        align-items: center !important;
        justify-content: center !important;
        box-sizing: border-box !important;
        height: var(--atf-category-badge-height) !important;
        min-height: var(--atf-category-badge-height) !important;
        line-height: 1 !important;
        vertical-align: middle !important;
        padding-top: 0 !important;
        padding-bottom: 0 !important;
      }
      .category-view .test-detail .meta,
      .category-view .test-contents > .info {
        align-items: center !important;
      }
      .category-view .tag-test-status a.linked {
        color: var(--atf-accent) !important;
        cursor: pointer !important;
        font-weight: 600 !important;
        text-decoration: none !important;
      }
      .category-view .tag-test-status a.linked:hover,
      .category-view .tag-test-status a.linked:focus {
        color: var(--atf-text) !important;
        text-decoration: underline !important;
      }
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
    """;
}

