namespace Atf.Reporting.Extent.Theme;

/// <summary>
/// Emits the renderer-side failure evidence projection. This fragment only
/// discovers typed execution evidence and creates deterministic failure
/// records; it does not create views, bind navigation or mutate the menu.
/// </summary>
internal static class ExtentReportClientFailureEvidenceFragments
{
    public static string Build() => """
            function compactText(value, limit) {
              const normalized = (value || '').replace(/\s+/g, ' ').trim();
              if (!normalized) return '';
              return normalized.length > limit ? normalized.slice(0, Math.max(0, limit - 1)) + '…' : normalized;
            }

            function evidenceKey(value, fallback) {
              const normalized = (value || '').toString().trim().toLowerCase();
              const slug = normalized.replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 96);
              return slug || fallback;
            }

            function nodeEvidenceKey(node, fallback) {
              if (!node) return fallback;
              const title = node.querySelector(':scope > .card-header .card-title > .node')?.textContent || node.textContent || '';
              return evidenceKey(node.id || title, fallback);
            }

            function failurePhase(card) {
              const phases = Array.from(card?.querySelectorAll('.card') || []).filter(function (candidate) {
                const title = candidate.querySelector(':scope > .card-header .card-title > .node')?.textContent || '';
                return Boolean(candidate.dataset.atfPhase || phaseKey(title));
              });
              return phases.find(function (phase) {
                return phase.querySelector('.fail-bg, .badge-danger, details.step-detail.fail');
              }) || phases.find(function (phase) {
                return /failed|failure|exception/i.test(phase.innerText || '');
              }) || phases[0] || null;
            }

            function failureCause(phase) {
              const source = phase || document.body;
              const pre = source.querySelector('details.step-detail.fail pre, .code-block');
              const value = pre?.value || pre?.textContent || '';
              const firstLine = value.split(/\r?\n/).map(function (line) { return line.trim(); }).find(Boolean);
              return compactText(firstLine || source.innerText, 180);
            }

            function executionPhaseFor(candidate, card) {
              let current = candidate;
              while (current && current !== card) {
                const title = current.querySelector(':scope > .card-header .card-title > .node')?.textContent || '';
                if (phaseKey(title)) return current;
                current = current.parentElement;
              }
              return failurePhase(card);
            }

            function isRequestCard(card) {
              const title = card?.querySelector(':scope > .card-header .card-title > .node')?.textContent?.trim() || '';
              return card?.dataset.atfNodeRole === 'request'
                || /^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS|TRACE)\b/i.test(title);
            }

            function isValidationCard(card) {
              const title = card?.querySelector(':scope > .card-header .card-title > .node')?.textContent?.trim() || '';
              return card?.dataset.atfNodeRole === 'validation' || /^validate\s*:/i.test(title);
            }

            function requestForValidation(validationCard, phase, card) {
              let current = validationCard?.previousElementSibling;
              while (current) {
                if (isRequestCard(current)) return current.querySelector(':scope > .card-header .card-title > .node');
                current = current.previousElementSibling;
              }
              return phase?.querySelector('[data-atf-node-role="request"] .card-title > .node')
                || card?.querySelector('[data-atf-node-role="request"] .card-title > .node')
                || Array.from((phase || card)?.querySelectorAll('.card-title > .node') || []).find(function (node) {
                  return /^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS|TRACE)\b/i.test(node.textContent?.trim() || '');
                });
            }

            function failureEvidence(card, assertionAnchor, assertionOrdinal) {
              const validationCard = assertionAnchor?.closest('.card') && isValidationCard(assertionAnchor.closest('.card'))
                ? assertionAnchor.closest('.card')
                : null;
              const phase = executionPhaseFor(validationCard || assertionAnchor, card);
              const request = requestForValidation(validationCard, phase, card);
              const requestCard = request?.closest('.card');
              const validation = validationCard?.querySelector(':scope > .card-header .card-title > .node')
                || phase?.querySelector('[data-atf-node-role="validation"] .card-title > .node')
                || card?.querySelector('[data-atf-node-role="validation"] .card-title > .node')
                || Array.from((phase || card)?.querySelectorAll('.card-title > .node') || []).find(function (node) {
                  return /^validate\s*:/i.test(node.textContent?.trim() || '');
                });
              const assertionRow = assertionAnchor?.matches('tr')
                ? assertionAnchor
                : phase?.querySelector('.helm-validation-table tbody tr, .helm-assertion-table tbody tr');
              const cells = assertionRow ? Array.from(assertionRow.children).map(function (cell) { return compactText(cell.innerText, 90); }) : [];
              const phaseId = nodeEvidenceKey(phase, 'phase');
              const requestId = nodeEvidenceKey(requestCard, evidenceKey(request?.textContent, 'request'));
              const validationCardId = nodeEvidenceKey(validationCard, evidenceKey(validation?.textContent, 'validation'));
              const assertionId = evidenceKey(cells[0], 'assertion-' + String(assertionOrdinal || 1));
              const evidenceId = [phaseId, requestId, validationCardId, assertionId].join('::');
              return {
                evidenceId: evidenceId,
                phaseId: phaseId,
                requestId: requestId,
                validationId: validationCardId,
                assertionId: assertionId,
                phase: phase ? (phase.dataset.atfPhase || phase.querySelector(':scope > .card-header .card-title > .node')?.textContent || '').toUpperCase() : 'UNKNOWN',
                request: compactText(request?.textContent, 120) || 'No HTTP request recorded',
                validation: compactText(validation?.textContent, 120) || 'No contract validation recorded',
                assertion: cells.length >= 3 ? cells.slice(0, 3).join(' → ') : '',
                cause: failureCause(phase || card)
              };
            }

            function failureEvidenceRecords(card) {
              const records = [];
              const phaseCards = Array.from(card?.querySelectorAll('.card') || []).filter(function (candidate) {
                const title = candidate.querySelector(':scope > .card-header .card-title > .node')?.textContent || '';
                return Boolean(phaseKey(title));
              });

              phaseCards.forEach(function (phase) {
                const container = phase.querySelector(':scope > .accordion');
                const children = Array.from(container?.children || []).filter(function (candidate) {
                  return candidate.classList.contains('card');
                });
                children.filter(isValidationCard).forEach(function (validationCard) {
                  const failedRows = Array.from(validationCard.querySelectorAll('tr[data-atf-assertion-status]'))
                    .filter(function (row) { return row.dataset.atfAssertionStatus === 'fail'; });
                  failedRows.forEach(function (row, rowOrdinal) {
                    records.push({ evidence: failureEvidence(card, row, rowOrdinal + 1), anchor: row });
                  });
                });
              });

              return records.length > 0
                ? records
                : [{ evidence: failureEvidence(card, null, 1), anchor: null }];
            }

    """;
}
