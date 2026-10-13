namespace Atf.Reporting.Extent.Theme.Client.Tags;

/// <summary>
/// Applies the configured renderer-neutral tag taxonomy to Extent's native
/// selector and exposes a compact, visible annotation strip per scenario.
/// It never changes the Extent test hierarchy or status counters.
/// </summary>
internal static class ExtentReportClientTagPolicyController
{
    public static string Build() => """
            const atfTagPolicy = Object.freeze({
              selection: new Set(__ATF_SELECTION_TAGS__),
              diagnostic: new Set(__ATF_DIAGNOSTIC_TAGS__)
            });

            function normalizedTag(value) {
              return (value || '').trim();
            }

            function tagKind(value) {
              const normalized = normalizedTag(value);
              return normalized && atfTagPolicy.selection.has(normalized)
                ? 'selection'
                : 'diagnostic';
            }

            function tagOrderRank(value) {
              const normalized = normalizedTag(value);
              const index = __ATF_TAG_ORDER__.indexOf(normalized);
              return index < 0 ? __ATF_TAG_ORDER__.length : index;
            }

            function annotationBadge(tag, kind) {
              const badge = document.createElement('span');
              badge.className = 'atf-tag-annotation atf-tag-annotation-' + kind;
              badge.setAttribute('role', 'listitem');
              badge.setAttribute('data-atf-tag-kind', kind);
              badge.textContent = tag;
              return badge;
            }

            function annotationStrip(tags, className, label) {
              const strip = document.createElement('div');
              strip.className = className;
              strip.setAttribute('role', 'list');
              strip.setAttribute('aria-label', label);
              tags.forEach(function (tag) {
                strip.appendChild(annotationBadge(tag, tagKind(tag)));
              });
              return strip;
            }

            function scenarioTags(card) {
              const attr = card.querySelector(':scope > .card-header .card-title > .node-attr');
              if (!attr) return [];
              return Array.from(attr.querySelectorAll(':scope > .badge'))
                .map(function (badge) { return normalizedTag(badge.textContent); })
                .filter(Boolean)
                .filter(function (tag, index, tags) {
                  return tags.indexOf(tag) === index;
                })
                .sort(function (left, right) {
                  return tagOrderRank(left) - tagOrderRank(right) || left.localeCompare(right);
                });
            }

            function configureTagSelector() {
              const selector = document.getElementById('tag-toggle');
              if (!selector || selector.dataset.atfTagPolicyConfigured === 'true') return;
              selector.querySelectorAll(':scope > .dropdown-item').forEach(function (item) {
                const tag = normalizedTag(item.textContent);
                if (tagKind(tag) !== 'selection') item.remove();
              });
              selector.dataset.atfTagPolicyConfigured = 'true';
            }

            function configureScenarioTagAnnotations() {
              document.querySelectorAll('.test-view .test-list-wrapper > .test-list-item > .test-item').forEach(function (item) {
                const cards = directScenarioCards(item);
                cards.forEach(function (card) {
                  const title = card.querySelector(':scope > .card-header .card-title');
                  if (!title || title.querySelector(':scope > .atf-scenario-annotations')) return;
                  const tags = scenarioTags(card);
                  if (tags.length > 0) {
                    title.appendChild(annotationStrip(tags, 'atf-scenario-annotations', 'Scenario annotations'));
                  }
                });
              });
            }

            function removeClassNavigationAnnotations() {
              // Class roots are navigation-only. Remove any annotation strip
              // that a native/previous client projection may have placed in
              // the left list; scenario cards remain the semantic tag owner.
              document.querySelectorAll('.test-view .test-list-wrapper > .test-list-item > .test-item > .test-detail > .atf-test-annotations')
                .forEach(function (strip) { strip.remove(); });
            }

            function configureTagPolicy() {
              configureTagSelector();
              removeClassNavigationAnnotations();
              configureScenarioTagAnnotations();
            }
            """;
}
