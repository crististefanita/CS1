namespace Atf.Reporting.Extent.Theme.Client.Dom;

/// <summary>
/// Emits a read-only DOM probe for the final report. It is diagnostic surface
/// only: it observes the hydrated document and never changes navigation,
/// status, disclosure state or report content.
/// </summary>
internal static class ExtentReportClientDomContractController
{
    public static string Build() => """
            window.__atfDomContract = Object.freeze({
              version: 1,
              inspect: function () {
                function text(element) {
                  return element ? (element.textContent || '').trim() : '';
                }

                function style(element) {
                  if (!element) return null;
                  const computed = window.getComputedStyle(element);
                  return {
                    display: computed.display,
                    alignItems: computed.alignItems,
                    color: computed.color,
                    backgroundColor: computed.backgroundColor,
                    borderColor: computed.borderColor,
                    fontSize: computed.fontSize,
                    overflowX: computed.overflowX,
                    overflowY: computed.overflowY
                  };
                }

                function rowSnapshot(row, status) {
                  const detail = row.querySelector(':scope > .test-detail');
                  const name = row.querySelector(':scope > .test-detail .name');
                  const bounds = row.getBoundingClientRect();
                  const detailBounds = detail && detail.getBoundingClientRect();
                  const statusBounds = status && status.getBoundingClientRect();
                  return {
                    identity: text(name),
                    status: text(status),
                    display: window.getComputedStyle(row).display,
                    statusPosition: status ? window.getComputedStyle(status).position : null,
                    statusRightGap: statusBounds ? bounds.right - statusBounds.right : null,
                    statusBesideIdentity: Boolean(statusBounds && detailBounds && statusBounds.left >= detailBounds.right),
                    statusOverlapsIdentity: Boolean(statusBounds && name && statusBounds.top < name.getBoundingClientRect().bottom && statusBounds.bottom > name.getBoundingClientRect().top),
                    style: style(row)
                  };
                }

                function testRows() {
                  return Array.from(document.querySelectorAll('.test-view .test-list-wrapper > .test-list-item > .test-item'))
                    .filter(function (row) { return row.offsetParent !== null; })
                    .map(function (row) {
                      return rowSnapshot(row, row.querySelector(':scope > .test-detail > .text-sm > .badge'));
                    });
                }

                function failureRows() {
                  return Array.from(document.querySelectorAll('.exception-view .test-list-wrapper > .test-list-item > .test-item'))
                    .filter(function (row) { return row.offsetParent !== null; })
                    .map(function (row) { return rowSnapshot(row, row.querySelector(':scope > .test-status')); });
                }

                const details = Array.from(document.querySelectorAll('details.step-detail'));
                const jsonBlock = document.querySelector('.helm-scroll-block-body');
                const assertionTable = document.querySelector('.helm-validation-table');
                return {
                  views: {
                    tests: testRows(),
                    fails: failureRows(),
                    active: document.querySelector('.main-content > .view:not(.d-none)')?.className || ''
                  },
                  disclosures: {
                    evidenceCount: details.length,
                    openEvidenceCount: details.filter(function (detail) { return detail.open; }).length,
                    json: jsonBlock ? {
                      overflowX: window.getComputedStyle(jsonBlock).overflowX,
                      overflowY: window.getComputedStyle(jsonBlock).overflowY,
                      style: style(jsonBlock)
                    } : null,
                    assertionTablePresent: Boolean(assertionTable)
                  },
                  overview: {
                    secondaryOpenCount: Array.from(document.querySelectorAll('.atf-secondary-overview[open]')).length,
                    tagsPresent: Boolean(document.querySelector('.category-container')),
                    environmentPresent: Boolean(document.querySelector('.system-view, .system-environment'))
                  }
                };
              }
            });
            """;
}
