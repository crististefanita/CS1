namespace Atf.Reporting.Extent.Theme.Client;

/// <summary>
/// Browser controller for independent card disclosure and collapsed defaults.
/// </summary>
internal static class ExtentReportClientDisclosureController
{
    public static string Build() => """
            function cardBodies(card) {
              if (!card) return [];
              const header = directChild(card, 'card-header');
              return Array.from(card.children).filter(function (child) {
                return child !== header;
              });
            }

            function toggleCard(card, header, event) {
              const bodies = cardBodies(header.parentElement);
              if (bodies.length === 0) return;
              event.preventDefault();
              event.stopImmediatePropagation();
              event.stopPropagation();
              const expanded = header.getAttribute('aria-expanded') !== 'true';
              bodies.forEach(function (body) {
                body.classList.toggle('show', expanded);
                body.style.setProperty('display', expanded ? 'block' : 'none', 'important');
              });
              header.setAttribute('aria-expanded', expanded ? 'true' : 'false');
            }

            function bindCardToggle(card, header) {
              if (card.dataset.atfToggleBound === 'true') return;
              card.dataset.atfToggleBound = 'true';
              const activate = function (event) {
                if (event.target.closest('.card-toolbar')) return;
                toggleCard(card, header, event);
              };
              header.__atfToggleHandler = activate;
              if (window.__atfCaptureToggleInstalled === true) return;
              window.__atfCaptureToggleInstalled = true;
              window.addEventListener('click', function (event) {
                const target = event.target instanceof Element ? event.target : null;
                const targetHeader = target?.closest('.test-view .card-header[role="button"]');
                if (!targetHeader || target.closest('.card-toolbar')) return;
                toggleCard(targetHeader.parentElement, targetHeader, event);
              }, true);
              window.addEventListener('keydown', function (event) {
                if (event.key !== 'Enter' && event.key !== ' ') return;
                const target = event.target instanceof Element ? event.target : null;
                const targetHeader = target?.closest('.test-view .card-header[role="button"]');
                if (!targetHeader || target.closest('.card-toolbar')) return;
                toggleCard(targetHeader.parentElement, targetHeader, event);
              }, true);
            }

            function installCardDelegation() {
              if (document.documentElement.dataset.atfCardDelegationInstalled === 'true') return;
              document.documentElement.dataset.atfCardDelegationInstalled = 'true';
              document.addEventListener('click', function (event) {
                const target = event.target instanceof Element ? event.target : null;
                const header = target?.closest('.test-view .accordion > .card > .card-header');
                if (!header || target.closest('.card-toolbar')) return;
                event.preventDefault();
                event.stopImmediatePropagation();
                toggleCard(header.parentElement, header, event);
              }, true);

              document.addEventListener('keydown', function (event) {
                if (event.key !== 'Enter' && event.key !== ' ') return;
                const target = event.target instanceof Element ? event.target : null;
                const header = target?.closest('.test-view .accordion > .card > .card-header');
                if (!header || target.closest('.card-toolbar')) return;
                event.preventDefault();
                event.stopImmediatePropagation();
                toggleCard(header.parentElement, header, event);
              }, true);
            }
            """;
}
