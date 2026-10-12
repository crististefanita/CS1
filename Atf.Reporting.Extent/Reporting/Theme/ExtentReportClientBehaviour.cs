using System.Text.Json;

namespace Atf.Reporting.Extent.Theme;

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
        var chartPalette = JsonSerializer.Serialize(new
        {
            pass = AtfReportColors.OverviewPass,
            fail = AtfReportColors.OverviewFail,
            warning = AtfReportColors.OverviewWarning,
            skip = AtfReportColors.OverviewSkip,
            info = AtfReportColors.OverviewInfo,
            other = AtfReportColors.OverviewOther
        });

        var script = """
        <script id="atf-custom-behaviour">
          (function () {
            const chartPalette = __ATF_CHART_PALETTE__;

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

            function compactText(value, limit) {
              const normalized = (value || '').replace(/\s+/g, ' ').trim();
              if (!normalized) return '';
              return normalized.length > limit ? normalized.slice(0, Math.max(0, limit - 1)) + '…' : normalized;
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

            function failureEvidence(card) {
              const phase = failurePhase(card);
              const request = phase?.querySelector('[data-atf-node-role="request"] .card-title > .node')
                || card?.querySelector('[data-atf-node-role="request"] .card-title > .node')
                || Array.from((phase || card)?.querySelectorAll('.card-title > .node') || []).find(function (node) {
                  return /^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS|TRACE)\b/i.test(node.textContent?.trim() || '');
                });
              const validation = phase?.querySelector('[data-atf-node-role="validation"] .card-title > .node')
                || card?.querySelector('[data-atf-node-role="validation"] .card-title > .node')
                || Array.from((phase || card)?.querySelectorAll('.card-title > .node') || []).find(function (node) {
                  return /^validate\s*:/i.test(node.textContent?.trim() || '');
                });
              const assertionRow = phase?.querySelector('.helm-validation-table tbody tr, .helm-assertion-table tbody tr');
              const cells = assertionRow ? Array.from(assertionRow.children).map(function (cell) { return compactText(cell.innerText, 90); }) : [];
              return {
                phase: phase ? (phase.dataset.atfPhase || phase.querySelector(':scope > .card-header .card-title > .node')?.textContent || '').toUpperCase() : 'UNKNOWN',
                request: compactText(request?.textContent, 120) || 'No HTTP request recorded',
                validation: compactText(validation?.textContent, 120) || 'No contract validation recorded',
                assertion: cells.length >= 3 ? cells.slice(0, 3).join(' → ') : '',
                cause: failureCause(phase || card)
              };
            }

            function failureTestCard(nodeId, visibleDetailOnly) {
              if (!nodeId) return null;
              const allNodes = Array.from(document.querySelectorAll('.test-view .node[id]'));
              const detailNodes = Array.from(document.querySelectorAll('.test-view .test-content-detail .node[id]'));
              const nodes = visibleDetailOnly ? detailNodes : allNodes;
              const node = nodes.find(function (candidate) {
                return candidate.id === nodeId;
              });
              return node?.closest('.card') || null;
            }

            function selectTestItem(listItem) {
              if (!listItem) return;
              document.querySelectorAll('.test-view .test-list-wrapper > .test-list-item > .test-item').forEach(function (item) {
                const selected = item === listItem;
                item.classList.toggle('active', selected);
                const contents = item.querySelector(':scope > .test-contents');
                if (contents) contents.classList.toggle('d-none', !selected);
              });
            }

            function openFailureTest(link) {
              const nodeId = link.getAttribute('data-atf-node-id') || link.getAttribute('id');
              const testId = link.getAttribute('data-atf-test-id') || link.getAttribute('test-id');
              const listItem = Array.from(document.querySelectorAll('.test-view .test-list-wrapper > .test-list-item > .test-item')).find(function (item) {
                return !testId || item.getAttribute('test-id') === testId;
              });
              toggleView('test-view');
              if (listItem) {
                selectTestItem(listItem);
                // Keep Extent's native projection: it moves the selected test
                // into the right-hand detail pane. Directly revealing
                // .test-contents would render the detail inside the left list.
                listItem.click();
                listItem.classList.add('active');
              }
              window.location.hash = 'test-id=' + (testId || '');
              // Extent's native click/hash handlers populate the right pane
              // asynchronously. Poll only that visible detail pane; the same
              // node also exists in the hidden source list and must not be used.
              const reveal = function (attempt) {
                configure();
                const target = failureTestCard(nodeId, true);
                if (!target && attempt < 12) {
                  setTimeout(function () { reveal(attempt + 1); }, 100);
                  return;
                }
                if (target) {
                  target.scrollIntoView({ behavior: 'auto', block: 'start' });
                  const header = directChild(target, 'card-header');
                  if (header && header.getAttribute('aria-expanded') !== 'true') {
                    toggleCard(target, header, { preventDefault: function () {}, stopImmediatePropagation: function () {}, stopPropagation: function () {} });
                  }
                }
              };
              setTimeout(function () { reveal(0); }, 250);
            }

            function installFailureNavigation() {
              if (document.documentElement.dataset.atfFailureNavigationInstalled === 'true') return;
              document.documentElement.dataset.atfFailureNavigationInstalled = 'true';
              // Extent clones the selected exception content into its detail
              // pane. Delegation keeps the link alive in both the source row
              // and that cloned, visible row.
              document.addEventListener('click', function (event) {
                const target = event.target instanceof Element
                  ? event.target.closest('.exception-view .atf-failure-open, .exception-view a.linked[data-atf-node-id]')
                  : null;
                if (!target) return;
                event.preventDefault();
                event.stopImmediatePropagation();
                openFailureTest(target);
              }, true);
            }

            function configureFailureView() {
              document.querySelectorAll('.exception-view .tag-test-status').forEach(function (row) {
                if (row.dataset.atfFailureEnhanced === 'true') return;
                const link = row.querySelector('a.linked');
                if (!link) return;
                const nodeId = link.getAttribute('id') || '';
                const testId = link.getAttribute('test-id') || '';
                const card = failureTestCard(nodeId);
                // Extent may render the Exception index before the selected
                // test has been copied into test-content-detail. Wait for the
                // mutation instead of publishing a misleading global cause.
                if (!card) return;
                row.dataset.atfFailureEnhanced = 'true';
                link.setAttribute('data-atf-node-id', nodeId);
                link.setAttribute('data-atf-test-id', testId);
                link.setAttribute('title', 'Open this exact test in Tests');
                link.addEventListener('click', function (event) {
                  event.preventDefault();
                  event.stopImmediatePropagation();
                  openFailureTest(link);
                }, true);

                const evidence = failureEvidence(card);
                const detail = document.createElement('div');
                detail.className = 'atf-failure-schematic';
                detail.setAttribute('data-atf-failure-details', 'true');
                const fields = [
                  ['PHASE', evidence.phase],
                  ['REQUEST', evidence.request],
                  ['VALIDATION', evidence.validation],
                  ['ASSERTION', evidence.assertion || 'Failure recorded; see exact test for full evidence'],
                  ['CAUSE', evidence.cause || 'Failure details are available in the exact test node']
                ];
                fields.forEach(function (field) {
                  const cell = document.createElement('div');
                  cell.className = 'atf-failure-field' + (field[0] === 'CAUSE' ? ' atf-failure-cause' : '');
                  const label = document.createElement('span');
                  label.className = 'atf-failure-label';
                  label.textContent = field[0];
                  const value = document.createElement('span');
                  value.className = 'atf-failure-value';
                  value.textContent = field[1];
                  cell.append(label, value);
                  detail.appendChild(cell);
                });
                const action = document.createElement('button');
                action.type = 'button';
                action.className = 'atf-failure-open';
                action.textContent = 'Open in Tests';
                action.setAttribute('title', 'Open the exact failing test in the Tests tab');
                action.setAttribute('data-atf-node-id', nodeId);
                action.setAttribute('data-atf-test-id', testId);
                action.addEventListener('click', function (event) {
                  event.preventDefault();
                  event.stopImmediatePropagation();
                  openFailureTest(link);
                }, true);
                detail.appendChild(action);
                const cell = row.querySelector('td:last-child');
                if (cell) cell.appendChild(detail);
              });
            }

            __ATF_CONFIGURE_EXECUTION_HIERARCHY__

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

              removeExecutionPhaseChart(dashboard);
              removeLogEventsChart(dashboard);

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
              updateOverviewCharts(totals);

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

            function removeExecutionPhaseChart(dashboard) {
              const card = dashboard.querySelector('#grandchild-analysis')?.closest('.card');
              if (!card) return;
              const column = card.closest('.col-md-3');
              const owner = column || card;
              if (owner.dataset.atfExecutionPhasesRemoved === 'true') return;
              owner.remove();
            }

            function removeLogEventsChart(dashboard) {
              const canvas = dashboard.querySelector('#events-analysis');
              if (!canvas) return;

              // The native Extent dashboard card is not an ATF evidence node.
              // Remove the chart instance before its column so the dashboard
              // no longer exposes a misleading aggregate events metric while
              // Info/Pass/Fail evidence inside test nodes remains untouched.
              const chartInstances = window.Chart && window.Chart.instances;
              if (chartInstances) {
                Object.keys(chartInstances).forEach(function (key) {
                  const chart = chartInstances[key];
                  if (chart?.canvas?.id === 'events-analysis') chart.destroy?.();
                });
              }

              const card = canvas.closest('.card');
              const column = card?.closest('.col-md-3');
              const owner = column || card;
              owner?.remove();
            }

            function updateOverviewCharts(totals) {
              const chartInstances = window.Chart && window.Chart.instances;
              if (!chartInstances) return;

              Object.keys(chartInstances).forEach(function (key) {
                const chart = chartInstances[key];
                const canvasId = chart?.canvas?.id;
                if (!chart || !canvasId || !chart.data?.datasets?.length) return;

                const dataset = chart.data.datasets[0];
                if (canvasId === 'parent-analysis') {
                  dataset.data = [totals.passed, totals.failed, totals.other, totals.skipped];
                  dataset.backgroundColor = [chartPalette.pass, chartPalette.fail, chartPalette.other, chartPalette.skip];
                } else if (canvasId === 'child-analysis') {
                  dataset.backgroundColor = [chartPalette.pass, chartPalette.fail, chartPalette.warning, chartPalette.skip, chartPalette.info];
                } else {
                  return;
                }

                dataset.hoverBackgroundColor = dataset.backgroundColor;
                dataset.borderColor = 'rgba(16, 23, 35, .92)';
                dataset.borderWidth = 2;
                chart.update();
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
              const totalTestsFailed = scenarioStatusTotals.failed;
              const totalTestsSkipped = scenarioStatusTotals.skipped;
              const totalTestsPassed = scenarioStatusTotals.passed;
              const totalTests = totalTestsPassed + totalTestsFailed + totalTestsSkipped + scenarioStatusTotals.other;
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
              const metricGrid = textElement('div', 'atf-secondary-grid', '');
              [['ATF tests in this run', totalTests], ['API test classes (Extent)', testItems.length], ['Total tests passed', totalTestsPassed], ['Total tests failed', totalTestsFailed], ['Total tests skipped', totalTestsSkipped]].forEach(function (metric) {
                const item = textElement('div', 'atf-secondary-metric', '');
                item.append(textElement('div', 'atf-secondary-metric-label', metric[0]), textElement('div', 'atf-secondary-metric-value', String(metric[1])));
                metricGrid.appendChild(item);
              });
              runtimeBody.appendChild(metricGrid);
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
              const tagGrid = textElement('div', 'atf-tag-grid', '');

              function buildAuditCard(key, title, headers, items, includeShare) {
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
                table.append(head, body);
                scroll.appendChild(table);
                cardBody.appendChild(scroll);
                card.append(cardHeader, cardBody);
                return card;
              }

              tagGrid.appendChild(buildAuditCard('classes', 'ATF scenario identities by test class', ['NAME', 'COUNT', 'SHARE'], scenarioIdentities, true));
              tagGrid.appendChild(buildAuditCard('packages', 'Test packages', ['NAME', 'COUNT', 'SHARE'], packageRows, true));
              const annotationRows = rows.map(function (values) {
                return { name: values[0], count: values.slice(1, 5).reduce(function (sum, value) {
                  const parsed = Number.parseInt(value, 10);
                  return sum + (Number.isFinite(parsed) ? parsed : 0);
                }, 0) };
              });
              tagGrid.appendChild(buildAuditCard('other', 'Other annotations / tags', ['NAME', 'COUNT'], annotationRows, false));
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
              installFailureNavigation();
              configureFailureView();
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
               // It also keeps the phase -> request + validation sibling hierarchy
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

        return script.Replace(
                "__ATF_CONFIGURE_EXECUTION_HIERARCHY__",
                ExtentReportClientFragments.BuildExecutionHierarchyFunction(),
                StringComparison.Ordinal)
            .Replace("__ATF_CLASS_ORDER__", classOrder, StringComparison.Ordinal)
            .Replace("__ATF_SCENARIO_ORDER__", scenarioOrder, StringComparison.Ordinal)
            .Replace("__ATF_TAG_ORDER__", tagOrder, StringComparison.Ordinal)
            .Replace("__ATF_CHART_PALETTE__", chartPalette, StringComparison.Ordinal);
    }
}
