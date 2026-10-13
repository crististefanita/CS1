namespace Atf.Reporting.Extent.Theme.Client;

/// <summary>
/// Browser controller for view routing and deterministic navigation ordering.
/// </summary>
internal static class ExtentReportClientNavigationController
{
    public static string Build() => """
            function directChild(element, className) {
              if (!element) return null;
              for (const child of element.children) {
                if (child.classList.contains(className)) return child;
              }
              return null;
            }

            function phaseKey(title) {
              const normalized = (title || '').trim().toUpperCase();
              if (normalized === 'SETUP') return 'setup';
              if (normalized === 'TEST BODY') return 'test-body';
              if (normalized === 'TEARDOWN') return 'teardown';
              return null;
            }

            function orderRank(value, order) {
              const normalized = (value || '').replace(/^[^A-Za-z0-9]+/, '').trim();
              const index = order.indexOf(normalized);
              return index < 0 ? order.length : index;
            }

            function reconcileFailureNavigationOrder() {
              const menu = document.querySelector('.side-nav .side-nav-menu');
              const failureItem = document.getElementById('nav-exception')?.closest('.nav-item');
              const overviewItem = document.getElementById('nav-dashboard')?.closest('.nav-item');
              if (!menu || !failureItem || !overviewItem || failureItem === overviewItem) return;
              menu.insertBefore(failureItem, overviewItem);
            }

            function configureNavigationOrder() {
              const list = document.querySelector('.test-view .test-list-wrapper > .test-list-item');
              if (!list) return;

              const classOrder = __ATF_CLASS_ORDER__;
              const scenarioOrder = __ATF_SCENARIO_ORDER__;
              const tagOrder = __ATF_TAG_ORDER__;
              const items = Array.from(list.children);
              items.forEach(function (item, index) { item.dataset.atfNavigationIndex = String(index); });

              if (classOrder.length > 0 && list.dataset.atfNavigationOrderConfigured !== 'true') {
                items.sort(function (left, right) {
                  const leftName = left.querySelector(':scope > .test-detail .name')?.textContent || '';
                  const rightName = right.querySelector(':scope > .test-detail .name')?.textContent || '';
                  return orderRank(leftName, classOrder) - orderRank(rightName, classOrder)
                    || Number(left.dataset.atfNavigationIndex) - Number(right.dataset.atfNavigationIndex);
                });
                items.forEach(function (item) { list.appendChild(item); });
                list.dataset.atfNavigationOrderConfigured = 'true';
              }

              if (scenarioOrder.length > 0) {
                const lifecycle = items.find(function (item) {
                  return (item.querySelector(':scope > .test-detail .name')?.textContent || '').trim() === 'ApiLifecycleAndCombinationsTests';
                });
                const scenarioList = lifecycle?.querySelector(':scope > .test-contents > .mt-4 > .accordion');
                if (scenarioList) {
                  const cards = Array.from(scenarioList.children);
                  cards.forEach(function (card, index) { card.dataset.atfNavigationIndex = String(index); });
                  const orderedCards = cards.slice().sort(function (left, right) {
                    const leftName = left.querySelector(':scope > .card-header .card-title > .node')?.textContent || '';
                    const rightName = right.querySelector(':scope > .card-header .card-title > .node')?.textContent || '';
                    return orderRank(leftName, scenarioOrder) - orderRank(rightName, scenarioOrder)
                      || Number(left.dataset.atfNavigationIndex) - Number(right.dataset.atfNavigationIndex);
                  });
                  if (orderedCards.some(function (card, index) { return card !== cards[index]; })) {
                    orderedCards.forEach(function (card) { scenarioList.appendChild(card); });
                  }
                }
              }

              if (tagOrder.length > 0) {
                document.querySelectorAll('.test-view .test-content-detail .detail-head .m-t-15, .test-view .test-content-detail .node-attr').forEach(function (container) {
                  const pills = Array.from(container.querySelectorAll(':scope > .badge'));
                  const orderedPills = pills.slice().sort(function (left, right) {
                    return orderRank(left.textContent, tagOrder) - orderRank(right.textContent, tagOrder);
                  });
                  if (orderedPills.some(function (pill, index) { return pill !== pills[index]; })) {
                    orderedPills.forEach(function (pill) { container.appendChild(pill); });
                  }
                });
              }
            }

            function configurePhaseCards() {
              document.querySelectorAll('.test-view .test-content-detail .card').forEach(function (card) {
                const header = directChild(card, 'card-header');
                const title = header && header.querySelector('.card-title .node');
                const phase = phaseKey(title && title.textContent);
                if (phase) card.setAttribute('data-atf-phase', phase);
              });
            }

            function installNavigation() {
              window.toggleView = function (view) {
                document.querySelectorAll('.main-content > .view').forEach(function (candidate) {
                  candidate.classList.toggle('d-none', !candidate.classList.contains(view));
                });
                const activeView = document.querySelector('.main-content > .' + view);
                if (activeView) renderFirstViewItem(activeView);
                setActiveNavigation(view);
                window.currentView = view;
              };

              // Keep each view's selected item isolated from Extent's global
              // projection when the user moves between Tests and Exceptions.
              document.addEventListener('click', function (event) {
                const target = event.target instanceof Element ? event.target : null;
                const item = target?.closest('.main-content > .view .test-list-wrapper > .test-list-item > .test-item');
                if (!item) return;
                const view = item.closest('.main-content > .view');
                if (!view) return;
                event.preventDefault();
                event.stopImmediatePropagation();
                renderViewItem(view, item);
              }, true);
            }
            """;
}
