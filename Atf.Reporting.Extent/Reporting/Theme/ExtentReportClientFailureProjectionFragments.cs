namespace Atf.Reporting.Extent.Theme;

/// <summary>
/// Owns the boundary between extracted execution evidence and the client-side
/// failure index. The projector discovers failing scenario cards and delegates
/// deterministic identity creation to the record factory; it does not render
/// DOM, route navigation or infer evidence fields.
/// </summary>
internal static class ExtentReportClientFailureProjectionFragments
{
    public static string Build() => """
            function failureIdFor(testId, nodeId, title, ordinal) {
              const source = [testId, nodeId || title, ordinal || '1'].join('|').toLowerCase();
              const slug = source.replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 140);
              return 'failure-' + (slug || 'unknown');
            }

            // Factory boundary: converts one extracted evidence item into one
            // stable, navigable failure record. It owns identity only; it does
            // not query the DOM or render the Fails view.
            function createFailureRecord(testId, testName, title, nodeId, scenarioCard, failure, scenarioOrdinal, evidenceOrdinal) {
              const failureId = failureIdFor(testId, nodeId, failure.evidence.evidenceId || title, scenarioOrdinal + '-' + evidenceOrdinal);
              scenarioCard.dataset.atfFailureId = failureId;
              scenarioCard.dataset.atfFailureIds = (scenarioCard.dataset.atfFailureIds || '')
                .split(',')
                .filter(Boolean)
                .concat(failureId)
                .join(',');
              return {
                testId: testId,
                testName: testName,
                title: title,
                nodeId: nodeId,
                failureId: failureId,
                evidenceId: failure.evidence.evidenceId,
                card: scenarioCard,
                evidence: failure.evidence
              };
            }

            // Projector boundary: discovers failing scenario cards and delegates
            // record identity to the factory. Rendering and navigation consume
            // the returned records but do not rebuild them.
            function projectFailureRecords() {
              const records = [];
              document.querySelectorAll('.test-view .test-list-wrapper > .test-list-item > .test-item').forEach(function (testItem) {
                const testId = testItem.getAttribute('test-id') || '';
                const testName = testItem.querySelector(':scope > .test-detail .name')?.textContent?.trim() || 'Unnamed API test';
                directScenarioCards(testItem).forEach(function (scenarioCard, ordinal) {
                  const status = scenarioCard.querySelector(':scope > .card-header .card-toolbar .badge')?.textContent?.trim().toLowerCase() || '';
                  if (!status.includes('fail')) return;

                  const title = scenarioCard.querySelector(':scope > .card-header .card-title > .node')?.textContent?.trim() || 'Unnamed failing scenario';
                  const nodeId = scenarioCard.querySelector(':scope > .card-header .card-title > .node[id]')?.id || '';
                  failureEvidenceRecords(scenarioCard).forEach(function (failure, evidenceOrdinal) {
                    records.push(createFailureRecord(testId, testName, title, nodeId, scenarioCard, failure, ordinal + 1, evidenceOrdinal + 1));
                  });
                });
              });
              return records;
            }

            function failureScenarioRecords() {
              return projectFailureRecords();
            }

    """;
}
