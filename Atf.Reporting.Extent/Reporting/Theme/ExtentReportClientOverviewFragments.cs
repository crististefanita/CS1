namespace Atf.Reporting.Extent.Theme;

/// <summary>
/// Owns client-side Overview projections: scenario aggregation, native chart
/// synchronization and the secondary tag/class/route audit. It receives the
/// already-rendered Extent DOM and never owns test navigation or step toggles.
/// </summary>
internal static class ExtentReportClientOverviewFragments
{
    public static string Build() => """
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
                  '<b data-tooltip="' + percent(totals.other) + '">' + totals.other + '</b> warnings';
              }

              const stepsCard = Array.from(dashboard.querySelectorAll('.card')).find(function (card) {
                return card.querySelector('.card-title')?.textContent.trim() === 'Steps';
              });
              const stepFooterLines = stepsCard?.querySelector('.card-footer')?.querySelectorAll('small') || [];
              if (stepFooterLines.length >= 2) {
                stepFooterLines[1].textContent = stepFooterLines[1].textContent.replace(/\bothers\b/gi, 'warnings');
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
              if (nativeTagTable) {
                const headerCells = nativeTagTable.querySelectorAll('thead th');
                if (headerCells.length >= 5) headerCells[4].textContent = 'WARNING';
              }
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
              [['ATF tests in this run', totalTests], ['API test classes (Extent)', testItems.length], ['Total tests passed', totalTestsPassed], ['Total tests failed', totalTestsFailed], ['Total tests skipped', totalTestsSkipped], ['Total tests warning', scenarioStatusTotals.other]].forEach(function (metric) {
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

    """;
}

