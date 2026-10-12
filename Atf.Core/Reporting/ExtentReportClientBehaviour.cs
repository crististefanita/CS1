using System.Globalization;

namespace Atf.Core.Reporting;

/// <summary>
/// Owns the browser-side interaction model of the generated Extent document.
/// Keeping this client behaviour outside the report lifecycle manager prevents
/// HTML/CSS/JavaScript concerns from leaking into test execution orchestration.
/// </summary>
internal static class ExtentReportClientBehaviour
{
    public static string Build(int configuredTestTotal)
    {
        var script = """
        <script id="atf-custom-behaviour">
          (function () {
            function directChild(element, className) {
              if (!element) return null;
              for (const child of element.children) {
                if (child.classList.contains(className)) return child;
              }
              return null;
            }

            function cardBodies(card) {
              if (!card) return [];
              const header = directChild(card, 'card-header');
              return Array.from(card.children).filter(function (child) {
                return child !== header;
              });
            }

            function phaseKey(title) {
              const normalized = (title || '').trim().toUpperCase();
              if (normalized === 'SETUP') return 'setup';
              if (normalized === 'TEST BODY') return 'test-body';
              if (normalized === 'TEARDOWN') return 'teardown';
              return null;
            }

            function configurePhaseCards() {
              document.querySelectorAll('.test-view .test-content-detail .card').forEach(function (card) {
                const header = directChild(card, 'card-header');
                const title = header && header.querySelector('.card-title .node');
                const phase = phaseKey(title && title.textContent);
                if (phase) card.setAttribute('data-atf-phase', phase);
              });
            }

            function buildTagAudit() {
              const dashboard = document.querySelector('.dashboard-view');
              const source = dashboard && dashboard.querySelector('.category-container table');
              const sourceRow = source && source.closest('.row');
              if (!dashboard || !source || !sourceRow || dashboard.querySelector('.atf-secondary-overview')) return;

              const rows = Array.from(source.querySelectorAll('tbody tr')).map(function (sourceItem) {
                return Array.from(sourceItem.cells).map(function (cell) { return cell.textContent.trim(); });
              }).filter(function (values) { return values.length >= 6; });

              const testItems = Array.from(document.querySelectorAll('.test-view .test-list-wrapper > .test-list-item > .test-item'));
              function directScenarioCards(item) {
                return Array.from(item.querySelectorAll(':scope > .test-contents > .mt-4 > .accordion > .card'));
              }
              const scenarioCards = testItems.reduce(function (cards, item) {
                return cards.concat(directScenarioCards(item));
              }, []);
              const configuredTestTotal = __ATF_TEST_TOTAL__;
              const scenarioStatusTotals = scenarioCards.reduce(function (totals, card) {
                const statusBadge = card.querySelector(':scope > .card-header .card-toolbar .badge');
                const status = (statusBadge?.textContent || '').trim().toLowerCase();
                if (status.includes('fail')) totals.failed++;
                else if (status.includes('skip')) totals.ignored++;
                else totals.passed++;
                return totals;
              }, { passed: 0, failed: 0, ignored: 0 });
              const totalTests = configuredTestTotal > 0 ? configuredTestTotal : scenarioCards.length;
              const totalTestsFailed = scenarioStatusTotals.failed;
              const totalTestsSkipped = scenarioStatusTotals.ignored;
              const totalTestsPassed = Math.max(totalTests - totalTestsFailed - totalTestsSkipped, 0);
              const systemInfo = new Map(Array.from(document.querySelectorAll('.sysenv-container table tbody tr')).map(function (item) {
                const cells = Array.from(item.cells).map(function (cell) { return cell.textContent.trim(); });
                return [cells[0], cells[1]];
              }).filter(function (pair) { return pair[0] && pair[1]; }));

              function packageNameFor(item) {
                return item.getAttribute('data-test-package')
                  || systemInfo.get('Test package')
                  || 'Atf.Api';
              }

              const scenarios = [];
              testItems.forEach(function (item) {
                const className = (item.querySelector(':scope > .test-detail .name') || item.querySelector(':scope > .test-detail'))?.textContent.trim() || 'ApiTestBase';
                const classTags = (item.getAttribute('tag') || '').split(/\s+/).filter(Boolean);
                const packageName = packageNameFor(item);
                const cards = directScenarioCards(item);

                cards.forEach(function (card) {
                  const nameElement = card.querySelector(':scope > .card-header .card-title > .node');
                  const name = nameElement?.textContent.trim() || 'Unnamed API scenario';
                  const tags = Array.from(card.querySelectorAll(':scope > .card-header .card-title > .node-attr .badge')).map(function (badge) {
                    return badge.textContent.trim();
                  }).filter(Boolean);
                  scenarios.push({ name: name, className: className, packageName: packageName, tags: tags.length ? tags : classTags });
                });

                if (cards.length === 0) {
                  scenarios.push({ name: className, className: className, packageName: packageName, tags: classTags });
                }
              });

              function grouped(records, selector) {
                const groups = new Map();
                records.forEach(function (record) {
                  const key = selector(record);
                  groups.set(key, (groups.get(key) || 0) + 1);
                });
                return Array.from(groups.entries()).map(function (entry) { return { name: entry[0], count: entry[1] }; });
              }

              const scenarioIdentities = grouped(scenarios, function (scenario) {
                return scenario.className + ' · ' + scenario.name;
              });
              const packageRows = grouped(scenarios, function (scenario) { return scenario.packageName; });

              function row(className) {
                const element = document.createElement('div');
                element.className = className;
                return element;
              }

              function textElement(tagName, className, text) {
                const element = document.createElement(tagName);
                element.className = className;
                element.textContent = text;
                return element;
              }

              const runtimeRow = row('row atf-secondary-row');
              const runtimeColumn = textElement('div', 'col-md-12', '');
              const runtime = document.createElement('details');
              runtime.className = 'atf-secondary-overview';
              const runtimeSummary = document.createElement('summary');
              runtimeSummary.append(
                textElement('span', 'atf-secondary-overview-title', 'ATF execution metadata (secondary)'),
                textElement('span', 'atf-secondary-overview-badge', 'secondary metadata'));
              const runtimeBody = textElement('div', 'atf-secondary-body', '');
              runtimeBody.appendChild(textElement('p', 'atf-secondary-note', 'Authority: native Extent cards, statuses and timeline remain the execution verdict. Extent overview counters can aggregate class-root containers in this hierarchy; they are not the ATF test-case total. The total below comes from the test assembly, while supporting/unit cases remain outside the API evidence tree.'));
              const metricGrid = textElement('div', 'atf-secondary-grid', '');
              [['ATF test cases (all)', totalTests], ['API test classes (Extent)', testItems.length], ['Total Tests passed', totalTestsPassed], ['Total Tests failed', totalTestsFailed], ['Total Tests skipped', totalTestsSkipped]].forEach(function (metric) {
                const item = textElement('div', 'atf-secondary-metric', '');
                item.append(textElement('div', 'atf-secondary-metric-label', metric[0]), textElement('div', 'atf-secondary-metric-value', String(metric[1])));
                metricGrid.appendChild(item);
              });
              runtimeBody.appendChild(metricGrid);
              runtimeBody.appendChild(textElement('p', 'atf-secondary-reading-rule', 'Reading rule: the all-tests total is discovered from the .NET test assembly. One direct scenario card equals one API test-case invocation, including each theory/data combination; class roots and feature groups are navigation context, not additional tests.'));
              runtime.append(runtimeSummary, runtimeBody);
              runtimeColumn.appendChild(runtime);
              runtimeRow.appendChild(runtimeColumn);

              const auditRow = row('row atf-secondary-row');
              const auditColumn = textElement('div', 'col-md-12', '');
              const audit = document.createElement('details');
              audit.className = 'atf-secondary-overview atf-tag-audit-disclosure';
              const auditSummary = document.createElement('summary');
              auditSummary.append(
                textElement('span', 'atf-secondary-overview-title', 'ATF tag audit (secondary navigation metadata)'),
                textElement('span', 'atf-secondary-overview-badge', 'collapsed by default'));
              const auditBody = textElement('div', 'atf-tag-audit-body', '');
              const auditHeading = textElement('div', 'atf-tag-audit-heading', '');
              auditHeading.appendChild(textElement('p', '', 'Deduplicated API navigation metadata by test scenario, package and additional tags. Native Extent totals remain authoritative.'));
              auditBody.appendChild(auditHeading);
              const tagGrid = textElement('div', 'atf-tag-grid', '');

              function buildAuditCard(key, title, headers, items, totalLabel, includeShare) {
                const card = textElement('div', 'card atf-tag-card atf-tag-card-' + key, '');
                const cardHeader = textElement('div', 'card-header', '');
                cardHeader.appendChild(textElement('p', '', title));
                const cardBody = textElement('div', 'card-body', '');
                const scroll = textElement('div', 'atf-tag-scroll', '');
                const table = textElement('table', 'table table-sm atf-tag-table', '');
                const head = document.createElement('thead');
                const headRow = document.createElement('tr');
                headers.forEach(function (header) { headRow.appendChild(textElement('th', '', header)); });
                head.appendChild(headRow);
                const body = document.createElement('tbody');
                const total = items.reduce(function (sum, item) { return sum + item.count; }, 0);
                items.forEach(function (item) {
                  const itemRow = document.createElement('tr');
                  [item.name, String(item.count)].concat(includeShare ? [Math.round(item.count / Math.max(total, 1) * 100) + '%'] : []).forEach(function (value) {
                    itemRow.appendChild(textElement('td', '', value));
                  });
                  body.appendChild(itemRow);
                });
                const totalRow = document.createElement('tr');
                totalRow.className = 'atf-tag-total';
                totalRow.appendChild(textElement('th', '', totalLabel));
                totalRow.appendChild(textElement('td', '', String(total)));
                if (includeShare) totalRow.appendChild(textElement('td', '', '100%'));
                body.appendChild(totalRow);
                table.append(head, body);
                scroll.appendChild(table);
                cardBody.appendChild(scroll);
                card.append(cardHeader, cardBody);
                return card;
              }

              tagGrid.appendChild(buildAuditCard('classes', 'ATF scenario identities by test class', ['NAME', 'COUNT', 'SHARE'], scenarioIdentities, 'ATF scenario identities total', true));
              tagGrid.appendChild(buildAuditCard('packages', 'Test packages', ['NAME', 'COUNT', 'SHARE'], packageRows, 'Test packages total', true));
              const annotationRows = rows.map(function (values) {
                return { name: values[0], count: values.slice(1, 5).reduce(function (sum, value) {
                  const parsed = Number.parseInt(value, 10);
                  return sum + (Number.isFinite(parsed) ? parsed : 0);
                }, 0) };
              });
              tagGrid.appendChild(buildAuditCard('other', 'Other annotations / tags', ['NAME', 'COUNT'], annotationRows, 'Other annotations / tags total', false));
              auditBody.appendChild(tagGrid);
              audit.append(auditSummary, auditBody);
              auditColumn.appendChild(audit);
              auditRow.appendChild(auditColumn);

              sourceRow.parentElement.insertBefore(runtimeRow, sourceRow);
              sourceRow.parentElement.insertBefore(auditRow, sourceRow);
            }

            function configure() {
              configurePhaseCards();
              document.querySelectorAll('.test-view .accordion > .card').forEach(function (card) {
                const header = directChild(card, 'card-header');
                const bodies = cardBodies(card);
                if (!header || bodies.length === 0 || card.dataset.atfExpansionConfigured === 'true') return;
                card.dataset.atfExpansionConfigured = 'true';
                header.setAttribute('role', 'button');
                header.setAttribute('tabindex', '0');
                bodies.forEach(function (body) {
                  body.classList.remove('show');
                  body.style.setProperty('display', 'none', 'important');
                });
                header.setAttribute('aria-expanded', 'false');
              });

              document.querySelectorAll('.test-view details.step-detail').forEach(function (detail) {
                if (detail.dataset.atfDefaultCollapsed === 'true') return;
                detail.open = false;
                detail.dataset.atfDefaultCollapsed = 'true';
              });
              buildTagAudit();
            }

            function findHeader(target) {
              const header = target && target.closest && target.closest('.test-view .accordion > .card > .card-header');
              return header && directChild(header.parentElement, 'card-header') === header ? header : null;
            }

            function toggleCard(event) {
              const header = findHeader(event.target);
              if (!header) return;
              const bodies = cardBodies(header.parentElement);
              if (bodies.length === 0) return;
              event.preventDefault();
              event.stopImmediatePropagation();
              const expanded = header.getAttribute('aria-expanded') !== 'true';
              bodies.forEach(function (body) {
                body.classList.toggle('show', expanded);
                body.style.setProperty('display', expanded ? 'block' : 'none', 'important');
              });
              header.setAttribute('aria-expanded', expanded ? 'true' : 'false');
            }

            function start() {
              if (typeof window.toggleView === 'function') {
                const extentToggleView = window.toggleView;
                window.toggleView = function (view) {
                  if (view === 'category-view') {
                    window.jQuery('.main-content>*').addClass('d-none');
                    window.jQuery('.category-view').removeClass('d-none');
                    window.currentView = view;
                    return;
                  }
                  return extentToggleView(view);
                };
              }
              configure();
              document.addEventListener('click', toggleCard, true);
              document.addEventListener('keydown', function (event) {
                if (event.key === 'Enter' || event.key === ' ') toggleCard(event);
              }, true);
              const root = document.querySelector('.test-view') || document.body;
              new MutationObserver(configure).observe(root, { childList: true, subtree: true });
              [50, 250, 750].forEach(function (delay) { setTimeout(configure, delay); });
            }

            if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
            else start();
          }());
        </script>
        """;

        return script.Replace(
            "__ATF_TEST_TOTAL__",
            configuredTestTotal.ToString(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }
}
