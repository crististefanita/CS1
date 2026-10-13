namespace Atf.Reporting.Extent.Theme.Client.Failure;

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

            // Typed client boundary: records are immutable value objects and the
            // index keeps a stable occurrence order. Rendering code can consume
            // the same array-like contract without rebuilding failure identity.
            class FailureRecord {
              constructor(values) {
                Object.assign(this, values);
                Object.freeze(this);
              }
            }

            class FailureIndex {
              constructor(records) {
                this.records = Object.freeze(Array.from(records || []));
                Object.freeze(this);
              }

              get length() { return this.records.length; }
              map(callback, thisArg) { return this.records.map(callback, thisArg); }
              forEach(callback, thisArg) { return this.records.forEach(callback, thisArg); }
              [Symbol.iterator]() { return this.records[Symbol.iterator](); }
            }

            // Factory boundary: converts one extracted evidence item into one
            // stable, navigable failure record. It owns identity only; it does
            // not query the DOM or render the Fails view.
            function createFailureRecord(testId, testName, title, nodeId, scenarioCard, failure, scenarioOrdinal, evidenceOrdinal) {
              const failureId = failureIdFor(testId, nodeId, failure.evidence.evidenceId || title, scenarioOrdinal + '-' + evidenceOrdinal);
              scenarioCard.dataset.atfFailureId = failureId;
              const failureIds = (scenarioCard.dataset.atfFailureIds || '')
                .split(',')
                .filter(Boolean);
              if (!failureIds.includes(failureId)) failureIds.push(failureId);
              scenarioCard.dataset.atfFailureIds = failureIds.join(',');
              return new FailureRecord({
                testId: testId,
                testName: testName,
                title: title,
                nodeId: nodeId,
                failureId: failureId,
                evidenceId: failure.evidence.evidenceId,
                deepLink: '#test-id=' + encodeURIComponent(String(testId)) + '&failure-id=' + encodeURIComponent(failureId),
                assertion: failure.evidence.assertion || '',
                expected: failure.evidence.expected || '',
                actual: failure.evidence.actual || '',
                cause: failure.evidence.cause || '',
                card: scenarioCard,
                evidence: failure.evidence
              });
            }

            // Discovery boundary: the only function allowed to query the
            // rendered Extent DOM. It emits neutral candidates for the typed
            // projector and does not create view elements or navigation.
            function collectFailureCandidates() {
              const candidates = [];
              document.querySelectorAll('.test-view .test-list-wrapper > .test-list-item > .test-item').forEach(function (testItem) {
                const testId = testItem.getAttribute('test-id') || '';
                const testName = testItem.querySelector(':scope > .test-detail .name')?.textContent?.trim() || 'Unnamed API test';
                directScenarioCards(testItem).forEach(function (scenarioCard, ordinal) {
                  const status = scenarioCard.querySelector(':scope > .card-header .card-toolbar .badge')?.textContent?.trim().toLowerCase() || '';
                  if (!status.includes('fail')) return;

                  const title = scenarioCard.querySelector(':scope > .card-header .card-title > .node')?.textContent?.trim() || 'Unnamed failing scenario';
                  const nodeId = scenarioCard.querySelector(':scope > .card-header .card-title > .node[id]')?.id || '';
                  failureEvidenceRecords(scenarioCard).forEach(function (failure, evidenceOrdinal) {
                    candidates.push({
                      testId: testId,
                      testName: testName,
                      title: title,
                      nodeId: nodeId,
                      scenarioCard: scenarioCard,
                      failure: failure,
                      scenarioOrdinal: ordinal + 1,
                      evidenceOrdinal: evidenceOrdinal + 1
                    });
                  });
                });
              });
              return candidates;
            }

            // Projector boundary: converts neutral candidates into typed,
            // immutable records. It never queries DOM or infers a verdict.
            function projectFailureRecords(candidates) {
              const records = (candidates || []).map(function (candidate) {
                return createFailureRecord(
                  candidate.testId,
                  candidate.testName,
                  candidate.title,
                  candidate.nodeId,
                  candidate.scenarioCard,
                  candidate.failure,
                  candidate.scenarioOrdinal,
                  candidate.evidenceOrdinal);
              });
              return new FailureIndex(records);
            }

            function failureScenarioRecords() {
              return projectFailureRecords(collectFailureCandidates());
            }

    """;
}
