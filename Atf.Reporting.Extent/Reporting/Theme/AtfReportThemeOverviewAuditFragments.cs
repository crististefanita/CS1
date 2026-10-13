namespace Atf.Reporting.Extent.Theme.Css;

/// <summary>Secondary metadata, tags and audit table rules.</summary>
internal static class AtfReportThemeOverviewAuditFragments
{
    public static string Build() => """
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
      .dashboard-view .atf-tag-card > .card-header p { margin: 0 !important; color: var(--atf-secondary-heading-text) !important; font-family: var(--atf-overview-font-family) !important; font-size: var(--atf-font-base-size) !important; font-weight: 400 !important; line-height: 1.35 !important; }
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
      /* The embedded Extent stylesheet has a more specific .table rule for
         cell line-height and border colour. Reassert one shared cell contract
         at equal-or-higher specificity so native Overview and ATF audit rows
         cannot drift apart when the vendor stylesheet changes. */
      .dashboard-view .atf-tag-card .atf-tag-table > thead > tr > th,
      .dashboard-view .atf-tag-card .atf-tag-table > tbody > tr > td,
      .dashboard-view .atf-tag-card .atf-tag-table > tbody > tr > th,
      .dashboard-view .category-container > .card > .card-body > table > thead > tr > th,
      .dashboard-view .category-container > .card > .card-body > table > tbody > tr > td,
      .dashboard-view .category-container > .card > .card-body > table > tbody > tr > th,
      .dashboard-view .sysenv-container > .card > .card-body > table > thead > tr > th,
      .dashboard-view .sysenv-container > .card > .card-body > table > tbody > tr > td,
      .dashboard-view .sysenv-container > .card > .card-body > table > tbody > tr > th {
        line-height: 1.32 !important;
        border-color: var(--atf-border) !important;
      }
    """;
}

