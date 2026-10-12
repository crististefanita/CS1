using System.Text.Json;

namespace Atf.Core.Reporting.Theme;

/// <summary>
/// Owns the browser-side interaction model of the generated Extent document.
/// Keeping this client behaviour outside the report lifecycle manager prevents
/// HTML/CSS/JavaScript concerns from leaking into test execution orchestration.
/// </summary>
internal static class ExtentReportClientBehaviour
{
    public static string Build(ReportingSettings? settings = null)
    {
        var classOrder = JsonSerializer.Serialize(settings?.TestClassOrder ?? []);
        var scenarioOrder = JsonSerializer.Serialize(settings?.TestScenarioOrder ?? []);
        var tagOrder = JsonSerializer.Serialize(settings?.TagOrder ?? []);

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

            function ownedAccordion(card) {
              const direct = directChild(card, 'accordion');
              if (direct) return direct;
              const collapse = directChild(card, 'collapse');
              const body = collapse && directChild(collapse, 'card-body');
              return body && directChild(body, 'accordion');
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

            function orderRank(value, order) {
              const normalized = (value || '').replace(/^[^A-Za-z0-9]+/, '').trim();
              const index = order.indexOf(normalized);
              return index < 0 ? order.length : index;
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

            function configureExecutionHierarchy() {
              // Mark the semantic levels explicitly so CSS and interaction
              // tests preserve the contract: phase -> request -> validation.
              // Response evidence and the single aggregate validation belong
              // to the request that produced the response.
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

                  stepCard.setAttribute('data-atf-node-role', 'request');
                  stepCard.setAttribute('data-atf-parent-phase', phaseCard.getAttribute('data-atf-phase') || '');
                  stepCard.setAttribute('data-atf-parent-node', 'phase');

                  const requestAccordion = ownedAccordion(stepCard);
                  if (!requestAccordion) return;
                  Array.from(requestAccordion.children).forEach(function (validationCard) {
                    if (!validationCard.classList.contains('card')) return;
                    const validationHeader = directChild(validationCard, 'card-header');
                    const validationTitle = validationHeader && validationHeader.querySelector('.card-title .node');
                    const validationText = validationTitle && validationTitle.textContent ? validationTitle.textContent.trim() : '';
                    if (!validationTitle || !/^validate\s*:/i.test(validationText)) return;
                    validationCard.setAttribute('data-atf-node-role', 'validation');
                    validationCard.setAttribute('data-atf-parent-node', 'request');
                    validationCard.setAttribute('data-atf-parent-phase', phaseCard.getAttribute('data-atf-phase') || '');
                  });
                });
              });
            }

            function removeCompositeScenarioSummary() {
              // A lifecycle scenario owns the meaningful SETUP / TEST BODY /
              // TEARDOWN cards. Extent also renders a generic parent event
              // table (usually just "Scenario passed") before those cards;
              // keeping both makes the execution order look duplicated and
              // pushes the actual phase evidence below an irrelevant row.
              document.querySelectorAll('.test-view .test-content-detail .card').forEach(function (card) {
                const accordion = directChild(card, 'accordion');
                if (!accordion) return;

                const hasPhaseChild = Array.from(accordion.children).some(function (child) {
                  const header = directChild(child, 'card-header');
                  const title = header && header.querySelector('.card-title .node');
                  return phaseKey(title && title.textContent) !== null;
                });
                if (!hasPhaseChild) return;

                const summary = directChild(card, 'collapse');
                if (summary) summary.remove();
                card.dataset.atfCompositeSummaryRemoved = 'true';
              });
            }

            function directScenarioCards(item) {
              if (!item) return [];
              const accordion = item.querySelector(':scope > .test-contents > .mt-4 > .accordion');
              return accordion
                ? Array.from(accordion.children).filter(function (child) {
                    return child.classList.contains('card');
                  })
                : [];
            }

            function reportScenarioData() {
              const testItems = Array.from(document.querySelectorAll('.test-view .test-list-wrapper > .test-list-item > .test-item'));
              const scenarioCards = testItems.reduce(function (cards, item) {
                return cards.concat(directScenarioCards(item));
              }, []);
              const statusTotals = scenarioCards.reduce(function (totals, card) {
                const statusBadge = card.querySelector(':scope > .card-header .card-toolbar .badge');
                const status = (statusBadge?.textContent || '').trim().toLowerCase();
                if (status.includes('fail')) totals.failed++;
                else if (status.includes('skip')) totals.skipped++;
                else if (status.includes('warning')) totals.other++;
                else if (status.includes('pass')) totals.passed++;
                else totals.other++;
                return totals;
              }, { passed: 0, failed: 0, skipped: 0, other: 0 });
              return { testItems: testItems, scenarioCards: scenarioCards, statusTotals: statusTotals };
            }

            function updateNativeOverview() {
              const dashboard = document.querySelector('.dashboard-view');
              if (!dashboard) return;

              const data = reportScenarioData();
              if (data.scenarioCards.length === 0) return;

              const totals = data.statusTotals;
              const total = data.scenarioCards.length;
              const percent = function (value) {
                return Math.round(value / Math.max(total, 1) * 100) + '%';
              };
              const summaryValue = function (label) {
                const paragraph = Array.from(dashboard.querySelectorAll('p')).find(function (candidate) {
                  return candidate.textContent.trim() === label;
                });
                return paragraph?.parentElement?.querySelector('h3');
              };
              const setText = function (element, value) {
                if (element) element.textContent = String(value);
              };

              // Extent's native parent counters count class roots. ATF keeps
              // those roots as navigation containers, so the visible overview
              // must count the direct scenario cards instead.
              setText(summaryValue('Tests Passed'), totals.passed);
              setText(summaryValue('Tests Failed'), totals.failed);

              const testsCard = Array.from(dashboard.querySelectorAll('.card')).find(function (card) {
                return card.querySelector('.card-title')?.textContent.trim() === 'Tests';
              });
              const testFooter = testsCard?.querySelector('.card-footer');
              const footerLines = testFooter ? testFooter.querySelectorAll('small') : [];
              if (footerLines.length >= 2) {
                footerLines[0].setAttribute('data-tooltip', percent(totals.passed));
                footerLines[0].innerHTML = '<b>' + totals.passed + '</b> tests passed';
                footerLines[1].setAttribute('data-tooltip', percent(totals.failed));
                footerLines[1].innerHTML = '<b>' + totals.failed + '</b> tests failed, ' +
                  '<b>' + totals.skipped + '</b> skipped, ' +
                  '<b data-tooltip="' + percent(totals.other) + '">' + totals.other + '</b> others';
              }

              if (typeof statusGroup !== 'undefined') {
                statusGroup.parentCount = total;
                statusGroup.passParent = totals.passed;
                statusGroup.failParent = totals.failed;
                statusGroup.warningParent = totals.other;
                statusGroup.skipParent = totals.skipped;
              }

              // Extent creates its charts before the ATF script is loaded.
              // Update the existing parent chart instead of adding another
              // canvas or changing the steps/events counters.
              const chartInstances = window.Chart && window.Chart.instances;
              if (chartInstances) {
                Object.keys(chartInstances).forEach(function (key) {
                  const chart = chartInstances[key];
                  if (chart?.canvas?.id !== 'parent-analysis') return;
                  chart.data.datasets[0].data = [totals.passed, totals.failed, totals.other, totals.skipped];
                  chart.update();
                });
              }

              // The native Tags table is also parent-oriented in Extent.
              // Reconcile its rows from the same direct scenario cards so a
              // user never sees class-root totals beside scenario totals.
              const tagTotals = new Map();
              data.scenarioCards.forEach(function (card) {
                const status = (card.querySelector(':scope > .card-header .card-toolbar .badge')?.textContent || '').trim().toLowerCase();
                const bucket = status.includes('fail') ? 'failed'
                  : status.includes('skip') ? 'skipped'
                    : status.includes('warning') ? 'other'
                      : status.includes('pass') ? 'passed' : 'other';
                const tags = Array.from(card.querySelectorAll(':scope > .card-header .card-title > .node-attr .badge'))
                  .map(function (badge) { return badge.textContent.trim(); })
                  .filter(Boolean);
                tags.forEach(function (tag) {
                  const totals = tagTotals.get(tag) || { passed: 0, failed: 0, skipped: 0, other: 0 };
                  totals[bucket]++;
                  tagTotals.set(tag, totals);
                });
              });
              const nativeTagTable = dashboard.querySelector('.category-container table');
              nativeTagTable?.querySelectorAll('tbody tr').forEach(function (row) {
                const cells = row.cells;
                const totals = tagTotals.get(cells[0]?.textContent.trim());
                if (!totals || cells.length < 6) return;
                cells[1].textContent = String(totals.passed);
                cells[2].textContent = String(totals.failed);
                cells[3].textContent = String(totals.skipped);
                cells[4].textContent = String(totals.other);
                const tagTotal = totals.passed + totals.failed + totals.skipped + totals.other;
                cells[5].textContent = Math.round(totals.passed / Math.max(tagTotal, 1) * 100) + '%';
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

              const reportData = reportScenarioData();
              const testItems = reportData.testItems;
              const scenarioCards = reportData.scenarioCards;
              const scenarioStatusTotals = reportData.statusTotals;
              const totalTests = scenarioCards.length;
              const totalTestsFailed = scenarioStatusTotals.failed;
              const totalTestsSkipped = scenarioStatusTotals.skipped;
              const totalTestsPassed = scenarioStatusTotals.passed;
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
              runtimeBody.appendChild(textElement('p', 'atf-secondary-note', 'Authority: native Extent cards, statuses and timeline remain the execution verdict. Overview counters count direct API scenario cards; class roots and supporting/unit cases are navigation or runner context, not additional API scenarios.'));
              const metricGrid = textElement('div', 'atf-secondary-grid', '');
              [['API test cases (Extent)', totalTests], ['API test classes (Extent)', testItems.length], ['API scenarios passed', totalTestsPassed], ['API scenarios failed', totalTestsFailed], ['API scenarios skipped', totalTestsSkipped]].forEach(function (metric) {
                const item = textElement('div', 'atf-secondary-metric', '');
                item.append(textElement('div', 'atf-secondary-metric-label', metric[0]), textElement('div', 'atf-secondary-metric-value', String(metric[1])));
                metricGrid.appendChild(item);
              });
              runtimeBody.appendChild(metricGrid);
              runtimeBody.appendChild(textElement('p', 'atf-secondary-reading-rule', 'Reading rule: one direct scenario card equals one API test-case invocation, including each theory/data combination. Class roots and feature groups are navigation context; unit/component tests remain outside this API evidence report.'));
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
              auditHeading.appendChild(textElement('p', '', 'Deduplicated API navigation metadata by test scenario, package and additional tags. Overview counters and direct scenario cards remain authoritative.'));
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
              configureNavigationOrder();
              configurePhaseCards();
              configureExecutionHierarchy();
              removeCompositeScenarioSummary();
              updateNativeOverview();
              document.querySelectorAll('.test-view .accordion > .card').forEach(function (card) {
                const header = directChild(card, 'card-header');
                const bodies = cardBodies(card);
                if (!header || bodies.length === 0) return;
                if (card.dataset.atfExpansionConfigured === 'true') return;
                card.dataset.atfExpansionConfigured = 'true';
                header.setAttribute('role', 'button');
                header.setAttribute('tabindex', '0');
                bodies.forEach(function (body) {
                  // Match the reference Extent report: the scenario is the
                  // navigation entry, while phase shells and API steps are
                  // compact collapsed nodes until the user opens them.
                  body.classList.remove('show');
                  body.style.setProperty('display', 'none', 'important');
                });
                header.setAttribute('aria-expanded', 'false');
                bindCardToggle(card, header);
              });

              document.querySelectorAll('.test-view details.step-detail').forEach(function (detail) {
                if (detail.dataset.atfDefaultCollapsed === 'true') return;
                detail.open = false;
                detail.dataset.atfDefaultCollapsed = 'true';
              });
              buildTagAudit();
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
                // Status badges and toolbar controls have their own native
                // actions. The complete header is the toggle surface: this
                // keeps the interaction reliable when Extent's nested
                // markup or CSS overlays the title element.
                if (event.target.closest('.card-toolbar')) return;
                toggleCard(card, header, event);
              };
              header.__atfToggleHandler = activate;
              if (window.__atfCaptureToggleInstalled === true) return;
              window.__atfCaptureToggleInstalled = true;
              // Extent's offline handler is attached above the generated
              // cards and can consume the event before a header listener.
              // Intercept at window capture, then stop propagation after ATF
              // has applied the disclosure state.
              window.addEventListener('click', function (event) {
                const target = event.target instanceof Element ? event.target : null;
                const targetHeader = target?.closest('.test-view .card-header[role="button"]');
                const handler = targetHeader?.__atfToggleHandler;
                if (typeof handler === 'function') handler(event);
              }, true);
              window.addEventListener('keydown', function (event) {
                if (event.key !== 'Enter' && event.key !== ' ') return;
                const target = event.target instanceof Element ? event.target : null;
                const targetHeader = target?.closest('.test-view .card-header[role="button"]');
                const handler = targetHeader?.__atfToggleHandler;
                if (typeof handler === 'function') handler(event);
              }, true);
            }

            function installCardDelegation() {
              if (document.documentElement.dataset.atfCardDelegationInstalled === 'true') return;
              document.documentElement.dataset.atfCardDelegationInstalled = 'true';

              // Extent attaches its own handlers to card headers. A delegated
              // capture handler gives the ATF disclosure contract precedence
              // even when a header is inserted or replaced after navigation.
              // It also keeps phase-level request/validation siblings
              // independent: only the clicked card's direct body is toggled.
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

            function installNavigation() {
              /* Extent's offline bundle keeps the inline nav markup but can
                 scope its original jQuery toggleView helper away. Expose a
                 small dependency-free equivalent so every generated report
                 keeps working when the user moves between Tests, Categories
                 and Overview. */
              window.toggleView = function (view) {
                document.querySelectorAll('.main-content > .view').forEach(function (candidate) {
                  candidate.classList.toggle('d-none', !candidate.classList.contains(view));
                });
                if (view === 'test-view') {
                  document.querySelectorAll('.test-item').forEach(function (item) {
                    item.classList.remove('d-none');
                  });
                }
                window.currentView = view;
              };
            }

            function start() {
              installNavigation();
              installCardDelegation();
              configure();
              const root = document.querySelector('.test-view') || document.body;
              new MutationObserver(configure).observe(root, { childList: true, subtree: true });
              [50, 250, 750].forEach(function (delay) { setTimeout(configure, delay); });
            }

            if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
            else start();
          }());
        </script>
        """;

        return script.Replace("__ATF_CLASS_ORDER__", classOrder, StringComparison.Ordinal)
            .Replace("__ATF_SCENARIO_ORDER__", scenarioOrder, StringComparison.Ordinal)
            .Replace("__ATF_TAG_ORDER__", tagOrder, StringComparison.Ordinal);
    }
}
