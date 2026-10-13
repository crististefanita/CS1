namespace Atf.Reporting.Extent.Theme.Client;

/// <summary>
/// Browser behaviour fragments grouped by semantic concern. The parent
/// builder remains responsible only for composing and injecting settings.
/// </summary>
internal static class ExtentReportClientFragments
{
    public static string BuildExecutionHierarchyFunction() => """
            function configureExecutionHierarchy() {
              // The canonical tree is phase -> request + validation sibling.
              // The ordered pair owns one response contract.
              document.querySelectorAll('.test-view .test-content-detail .card[data-atf-phase]').forEach(function (phaseCard) {
                phaseCard.setAttribute('data-atf-node-role', 'phase');
                const phaseAccordion = directChild(phaseCard, 'accordion');
                if (!phaseAccordion) return;
                phaseAccordion.setAttribute('data-atf-node-group', 'phase-children');

                Array.from(phaseAccordion.children).forEach(function (stepCard) {
                  if (!stepCard.classList.contains('card')) return;
                  const stepHeader = directChild(stepCard, 'card-header');
                  const stepTitle = stepHeader && stepHeader.querySelector('.card-title .node');
                  const titleText = stepTitle && stepTitle.textContent ? stepTitle.textContent.trim() : '';
                  if (!stepTitle || phaseKey(titleText)) return;
                  if (!/^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS|TRACE)\b/i.test(titleText)) return;
                  stepCard.setAttribute('data-atf-node-role', 'request');
                  stepCard.setAttribute('data-atf-parent-phase', phaseCard.getAttribute('data-atf-phase') || '');
                  stepCard.setAttribute('data-atf-parent-node', 'phase');

                });

                Array.from(phaseAccordion.children).forEach(function (validationCard) {
                  if (!validationCard.classList.contains('card')) return;
                  const validationHeader = directChild(validationCard, 'card-header');
                  const validationTitle = validationHeader && validationHeader.querySelector('.card-title .node');
                  const validationText = validationTitle && validationTitle.textContent
                    ? validationTitle.textContent.trim()
                    : '';
                  if (!/^validate\s*:/i.test(validationText)) return;
                  validationCard.setAttribute('data-atf-node-role', 'validation');
                  validationCard.setAttribute('data-atf-parent-phase', phaseCard.getAttribute('data-atf-phase') || '');
                  validationCard.setAttribute('data-atf-parent-node', 'phase');
                });
              });
            }
    """;
}
