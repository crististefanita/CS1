using Atf.Core.Reporting;
using Atf.Core.Reporting.Evidence;
using System.Net;

namespace Atf.Reporting.Extent.Evidence;

/// <summary>
/// Renders the Extent HTML contract for aggregated assertion evidence.
/// The assertion row model remains in Atf.Core; markup ownership belongs to
/// the renderer adapter.
/// </summary>
public static class AssertionEvidenceFormatter
{
    public static string BuildTableHtml(
        IReadOnlyCollection<AtfAssertion> assertions,
        int maxDisplayedCharacters = 0)
    {
        var rows = assertions.Count == 0
            ? "<tr><td colspan='4' class='muted'>No assertions were registered.</td></tr>"
            : string.Join(string.Empty, assertions.Select(assertion =>
            {
                var resultStatus = assertion.ResultStatus ?? (assertion.Passed
                    ? AtfReportStatus.Pass
                    : AtfReportStatus.Fail);
                var result = resultStatus switch
                {
                    AtfReportStatus.Warning => "<span class='assertion-result assertion-result-warning' role='img' aria-label='Warning' title='Warning'>⚠️</span>",
                    AtfReportStatus.Pass => "<span class='assertion-result assertion-result-pass' role='img' aria-label='Pass' title='Pass'>✅</span>",
                    AtfReportStatus.Skip => "<span class='assertion-result assertion-result-skip' role='img' aria-label='Skip' title='Skip'>⏭️</span>",
                    _ => "<span class='assertion-result assertion-result-fail' role='img' aria-label='Fail' title='Fail'>❌</span>"
                };

                var rowClass = resultStatus switch
                {
                    AtfReportStatus.Pass => "assertion-pass",
                    AtfReportStatus.Warning => "assertion-warning",
                    AtfReportStatus.Skip => "assertion-skip",
                    _ => "assertion-fail"
                };

                var expected = Display(assertion.Expected, maxDisplayedCharacters);
                var actual = Display(assertion.Actual, maxDisplayedCharacters);
                return $"<tr class='{rowClass}' data-atf-assertion-status='{resultStatus.ToString().ToLowerInvariant()}'>" +
                    $"<td>{Html(assertion.Name)}</td>" +
                    $"<td>{Html(expected)}</td>" +
                    $"<td>{Html(actual)}</td>" +
                    $"<td>{result}</td></tr>";
            }));

        return "<div class='helm-evidence-table-wrap'><table class='helm-evidence-table helm-validation-table'>" +
            "<thead><tr><th>ASSERTION</th><th>EXPECTED</th><th>ACTUAL</th><th>RESULT</th></tr></thead>" +
            $"<tbody>{rows}</tbody></table></div>";
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string Display(string value, int maxCharacters) =>
        maxCharacters <= 0 || value.Length <= maxCharacters
            ? value
            : value[..Math.Max(0, maxCharacters - 1)] + "…";
}
