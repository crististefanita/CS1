namespace Atf.Reporting.Extent.Theme.Css;

/// <summary>
/// Owns request/response metadata, technical trace, assertion tables and JSON
/// evidence surfaces. Keeping this fragment independent protects the report's
/// primary evidence layout from Overview or navigation changes.
/// </summary>
internal static class AtfReportThemeEvidenceFragments
{
    public static string Build() => """
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
      .helm-validation-table tr.assertion-warning td { background: __ATF_WARNING10__ !important; }
      .helm-validation-table tr.assertion-skip td { background: __ATF_WHITE03__ !important; opacity: .86 !important; }
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
    
    """;
}

