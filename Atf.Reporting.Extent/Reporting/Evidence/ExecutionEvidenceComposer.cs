namespace Atf.Reporting.Extent.Evidence;

/// <summary>
/// Classifies and renders one Extent evidence block. HTTP metadata and
/// assertion semantics are supplied by Core; this adapter owns the HTML.
/// </summary>
public sealed record ExecutionEvidence(
    string Title,
    string Kind,
    string BodyHtml,
    bool IsAssertion)
{
    public bool IsGenericSummary =>
        !IsAssertion && Title.StartsWith("Execution summary", StringComparison.OrdinalIgnoreCase);
}

public static class ExecutionEvidenceComposer
{
    public static ExecutionEvidence Compose(
        string stepTitle,
        IReadOnlyCollection<KeyValuePair<string, string>> details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepTitle);
        ArgumentNullException.ThrowIfNull(details);

        var expected = details.FirstOrDefault(detail =>
            detail.Key.Equals("expected", StringComparison.OrdinalIgnoreCase));
        var actual = details.FirstOrDefault(detail =>
            detail.Key.Equals("actual", StringComparison.OrdinalIgnoreCase));
        var isAssertion = expected.Key is not null && actual.Key is not null;
        var hasRequestMetadata = details.Any(detail =>
            detail.Key.Equals("method", StringComparison.OrdinalIgnoreCase) ||
            detail.Key.Equals("path", StringComparison.OrdinalIgnoreCase) ||
            detail.Key.Equals("url", StringComparison.OrdinalIgnoreCase));
        var hasResponseMetadata = details.Any(detail =>
            detail.Key.StartsWith("response.", StringComparison.OrdinalIgnoreCase));
        var evidenceKind = isAssertion
            ? string.Empty
            : hasResponseMetadata ? "response-details" : hasRequestMetadata ? "request-details" : string.Empty;

        var compactDetails = isAssertion
            ? Array.Empty<KeyValuePair<string, string>>()
            : hasRequestMetadata || hasResponseMetadata
                ? EvidenceTableFormatter.CompactTechnicalTraceDetails(details)
                : details.ToArray();
        var title = isAssertion
            ? "Assertion details"
            : hasResponseMetadata ? $"Response details · {compactDetails.Count} fields"
            : hasRequestMetadata ? $"Request details · {compactDetails.Count} fields"
            : $"Execution summary · {details.Count} fields";
        var table = isAssertion
            ? AssertionTable(stepTitle, expected.Value, actual.Value)
            : EvidenceTableFormatter.BuildTechnicalTraceTableHtml(compactDetails, compact: false);

        return new(title, evidenceKind, table, isAssertion);
    }

    private static string AssertionTable(string name, string expected, string actual) =>
        "<table class='helm-evidence-table helm-assertion-table'><thead>" +
        "<tr><th>ASSERTION</th><th>EXPECTED</th><th>ACTUAL</th></tr></thead>" +
        $"<tbody><tr><td>{Html(name)}</td><td>{Html(expected)}</td><td>{Html(actual)}</td></tr></tbody></table>";

    private static string Html(string value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
