namespace Atf.Reporting.Extent.Theme;

/// <summary>
/// Owns failure-index view projection and the deterministic navigation bridge
/// back to the exact test. Failure discovery and evidence extraction live in
/// <see cref="ExtentReportClientFailureEvidenceFragments"/> so this fragment
/// only renders and routes already-typed failure records.
/// </summary>
internal static class ExtentReportClientFailureFragments
{
    public static string Build() => ExtentReportClientFailureEvidenceFragments.Build()
        + ExtentReportClientFailureProjectionFragments.Build() + """
            function failureTestCard(nodeId, visibleDetailOnly, failureId) {
              const allNodes = Array.from(document.querySelectorAll('.test-view .node[id]'));
              const detailNodes = Array.from(document.querySelectorAll('.test-view .test-content-detail .node[id]'));
              const nodes = visibleDetailOnly ? detailNodes : allNodes;
              const node = nodeId && nodes.find(function (candidate) {
                return candidate.id === nodeId;
              });
              if (node) return node.closest('.card');
              if (!failureId) return null;
              return Array.from(document.querySelectorAll('.test-view .card[data-atf-failure-id]'))
                .find(function (card) {
                  return card.dataset.atfFailureId === failureId
                    || (card.dataset.atfFailureIds || '').split(',').includes(failureId);
                }) || null;
            }

            function renderViewItem(view, listItem) {
              if (!view || !listItem) return;
              const items = Array.from(view.querySelectorAll(':scope > .test-list > .test-list-wrapper > .test-list-item > .test-item'));
              items.forEach(function (item) { item.classList.toggle('active', item === listItem); });

              const detailBody = view.querySelector(':scope > .test-content > .test-content-detail > .detail-body');
              const contents = listItem.querySelector(':scope > .test-contents');
              if (!detailBody || !contents) return;

              const clone = contents.cloneNode(true);
              clone.classList.remove('d-none');
              detailBody.replaceChildren(clone);
            }

            function renderFirstViewItem(view) {
              const firstItem = view?.querySelector(':scope > .test-list > .test-list-wrapper > .test-list-item > .test-item');
              if (firstItem) renderViewItem(view, firstItem);
            }

            function failureTextElement(tagName, className, value) {
              const element = document.createElement(tagName);
              element.className = className;
              element.textContent = value || '';
              return element;
            }

            function createFailureViewItem(record) {
              const item = document.createElement('div');
              item.className = 'test-item';
              item.setAttribute('status', 'fail');
              item.setAttribute('test-id', record.testId);
              item.setAttribute('data-atf-failure-id', record.failureId);
              item.setAttribute('data-atf-evidence-id', record.evidenceId || record.evidence?.evidenceId || '');

              const detail = document.createElement('div');
              detail.className = 'test-detail';
              detail.append(
                failureTextElement('p', 'name', record.title),
                failureTextElement('p', 'text-sm', record.testName));

              const status = document.createElement('div');
              status.className = 'test-status';
              status.appendChild(failureTextElement('span', 'badge log badge-danger', 'Fail'));

              const contents = document.createElement('div');
              contents.className = 'test-contents d-none';
              const evidence = record.evidence || failureEvidence(record.card);
              const panel = document.createElement('div');
              panel.className = 'atf-failure-index';
              panel.setAttribute('data-atf-evidence-id', record.evidenceId || record.evidence?.evidenceId || '');
              panel.appendChild(failureTextElement('h4', 'atf-failure-index-title', record.title));

              [['PHASE', evidence.phase], ['REQUEST', evidence.request], ['VALIDATION', evidence.validation],
                ['ASSERTION', evidence.assertion || 'Failure recorded; see exact test for full evidence'],
                ['CAUSE', evidence.cause || 'Failure details are available in the exact test node']]
                .forEach(function (field) {
                  const row = document.createElement('div');
                  row.className = 'atf-failure-index-row' + (field[0] === 'CAUSE' ? ' atf-failure-index-cause' : '');
                  row.append(failureTextElement('span', 'atf-failure-label', field[0]), failureTextElement('span', 'atf-failure-value', field[1]));
                  panel.appendChild(row);
                });

              const action = document.createElement('button');
              action.type = 'button';
              action.className = 'atf-failure-open';
              action.textContent = 'Open in Tests';
              action.title = 'Open the exact failing test in the Tests tab';
              action.setAttribute('data-atf-node-id', record.nodeId);
              action.setAttribute('data-atf-test-id', record.testId);
              action.setAttribute('data-atf-failure-id', record.failureId);
              action.setAttribute('data-atf-evidence-id', record.evidenceId || evidence.evidenceId || '');
              panel.appendChild(action);
              contents.appendChild(panel);
              item.append(detail, status, contents);
              return item;
            }

            function ensureFailureNavigation() {
              const records = failureScenarioRecords();
              const existingView = document.querySelector('.main-content > .exception-view');
              const existingNav = document.getElementById('nav-exception')?.closest('.nav-item');
              if (records.length === 0) {
                existingNav?.remove();
                existingView?.remove();
                return;
              }

              const menu = document.querySelector('.side-nav .side-nav-menu');
              if (!menu) return;
              let navItem = existingNav;
              if (!navItem) {
                navItem = document.createElement('li');
                navItem.className = 'nav-item dropdown';
                const link = document.createElement('a');
                link.id = 'nav-exception';
                link.className = 'dropdown-toggle';
                link.href = '#exception-view';
                link.title = 'Fails';
                link.setAttribute('aria-label', 'Fails');
                link.innerHTML = '<span class="ico"><i class="fa fa-bug"></i></span>';
                link.addEventListener('click', function (event) {
                  event.preventDefault();
                  event.stopPropagation();
                  toggleView('exception-view');
                  window.location.hash = 'exception-view';
                });
                navItem.appendChild(link);
                menu.appendChild(navItem);
              }

              let view = existingView;
              if (!view) {
                view = document.createElement('div');
                view.className = 'test-wrapper row view exception-view d-none';
                view.innerHTML = '<div class="test-list"><div class="test-list-tools"><ul class="tools"><li><a href="#"><span class="font-size-14">Fails</span></a></li></ul></div><div class="test-list-wrapper"><div class="test-list-item"></div></div></div><div class="test-content"><div class="test-content-detail"><div class="detail-body"></div></div></div>';
                document.querySelector('.main-content')?.appendChild(view);
              }

              const list = view.querySelector(':scope > .test-list > .test-list-wrapper > .test-list-item');
              if (!list) return;
              list.replaceChildren(...records.map(createFailureViewItem));
              reconcileFailureNavigationOrder();
              if (!view.dataset.atfFailureViewConfigured) {
                view.dataset.atfFailureViewConfigured = 'true';
                renderFirstViewItem(view);
              }
            }

            function setActiveNavigation(view) {
              const navByView = {
                'test-view': 'nav-test',
                'category-view': 'nav-category',
                'exception-view': 'nav-exception',
                'dashboard-view': 'nav-dashboard'
              };
              const activeId = navByView[view];
              document.querySelectorAll('.side-nav .nav-item').forEach(function (item) {
                item.classList.toggle('active', item.querySelector('a[id]')?.id === activeId);
              });
            }

            function openTestById(testId) {
              if (!testId) return;
              const view = document.querySelector('.test-view');
              const listItem = Array.from(view?.querySelectorAll('.test-list-wrapper > .test-list-item > .test-item') || [])
                .find(function (item) { return item.getAttribute('test-id') === String(testId); });
              toggleView('test-view');
              if (view && listItem) renderViewItem(view, listItem);
              window.location.hash = 'test-id=' + encodeURIComponent(String(testId));
            }

            function configureCategoryLinks() {
              document.querySelectorAll('.category-view .tag-test-status a.linked').forEach(function (link) {
                const testId = link.getAttribute('test-id') || link.getAttribute('id');
                if (!testId) return;
                link.setAttribute('href', '#test-id=' + encodeURIComponent(testId));
                link.setAttribute('data-atf-test-id', testId);
                link.setAttribute('title', 'Open this test in Tests');
              });
            }

            function installCategoryNavigation() {
              if (document.documentElement.dataset.atfCategoryNavigationInstalled === 'true') return;
              document.documentElement.dataset.atfCategoryNavigationInstalled = 'true';
              document.addEventListener('click', function (event) {
                const target = event.target instanceof Element
                  ? event.target.closest('.category-view .tag-test-status a.linked[data-atf-test-id]')
                  : null;
                if (!target) return;
                event.preventDefault();
                event.stopImmediatePropagation();
                openTestById(target.getAttribute('data-atf-test-id'));
              }, true);
            }

            function initialView() {
              const hash = window.location.hash || '';
              if (/test-id=/i.test(hash)) return 'test-view';
              if (/category-view/i.test(hash)) return 'category-view';
              if (/exception-view/i.test(hash)) return 'exception-view';
              return 'dashboard-view';
            }

            function openInitialView() {
              const hash = window.location.hash || '';
              const testId = hash.match(/test-id=([^&]+)/i)?.[1];
              if (testId) {
                openTestById(decodeURIComponent(testId));
                return;
              }
              toggleView(initialView());
            }

            function openFailureTest(link) {
              const nodeId = link.getAttribute('data-atf-node-id') || link.getAttribute('id');
              const testId = link.getAttribute('data-atf-test-id') || link.getAttribute('test-id');
              const failureId = link.getAttribute('data-atf-failure-id') || '';
              const listItem = Array.from(document.querySelectorAll('.test-view .test-list-wrapper > .test-list-item > .test-item')).find(function (item) {
                return !testId || item.getAttribute('test-id') === testId;
              });
              toggleView('test-view');
              if (listItem) {
                // Never call Extent's native .test-item click here: its
                // global handler clones the source payload into every view,
                // which pollutes the left navigation and the Exception pane.
                renderViewItem(document.querySelector('.test-view'), listItem);
              }
              window.location.hash = 'test-id=' + (testId || '') + (failureId ? '&failure-id=' + encodeURIComponent(failureId) : '');
              // Extent's native click/hash handlers populate the right pane
              // asynchronously. Poll only that visible detail pane; the same
              // node also exists in the hidden source list and must not be used.
              const reveal = function (attempt) {
                configure();
                const target = failureTestCard(nodeId, true, failureId);
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
                const failureId = failureIdFor(testId, nodeId, link.textContent || 'failure', 1);
                const card = failureTestCard(nodeId, false, failureId);
                // Extent may render the Exception index before the selected
                // test has been copied into test-content-detail. Wait for the
                // mutation instead of publishing a misleading global cause.
                if (!card) return;
                const evidence = failureEvidence(card);
                row.dataset.atfFailureEnhanced = 'true';
                row.dataset.atfFailureId = failureId;
                row.dataset.atfEvidenceId = evidence.evidenceId || '';
                link.setAttribute('data-atf-node-id', nodeId);
                link.setAttribute('data-atf-test-id', testId);
                link.setAttribute('data-atf-failure-id', failureId);
                link.setAttribute('title', 'Open this exact test in Tests');
                link.addEventListener('click', function (event) {
                  event.preventDefault();
                  event.stopImmediatePropagation();
                  openFailureTest(link);
                }, true);

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
                action.setAttribute('data-atf-failure-id', failureId);
                action.setAttribute('data-atf-evidence-id', evidence.evidenceId || '');
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

    """;
}

