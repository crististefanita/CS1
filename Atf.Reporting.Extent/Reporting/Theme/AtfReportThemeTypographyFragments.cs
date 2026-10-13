namespace Atf.Reporting.Extent.Theme.Css;

/// <summary>Typography, evidence sizing, compact nodes and responsive rules.</summary>
internal static class AtfReportThemeTypographyFragments
{
    public static string Build() => """
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
    """;
}

