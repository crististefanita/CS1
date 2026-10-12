using System.Net;

namespace Atf.Core.Reporting.Evidence;

/// <summary>
/// Produces the stable, framework-neutral HTML contract for assertion evidence.
/// Keeping this separate makes the table independently unit-testable.
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
                var result = assertion.Passed
                    ? "<span class='assertion-result assertion-result-pass' role='img' aria-label='Pass' title='Pass'>✅</span>"
                    : "<span class='assertion-result assertion-result-fail' role='img' aria-label='Fail' title='Fail'>❌</span>";

                var expected = Display(assertion.Expected, maxDisplayedCharacters);
                var actual = Display(assertion.Actual, maxDisplayedCharacters);
                return $"<tr class='{(assertion.Passed ? "assertion-pass" : "assertion-fail")}'>" +
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
