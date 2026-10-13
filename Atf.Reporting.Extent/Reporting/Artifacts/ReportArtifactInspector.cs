using System.Net;
using System.Text.RegularExpressions;

namespace Atf.Reporting.Extent.Artifacts;

/// <summary>
/// Executable contract for the final Extent HTML artifact. It deliberately
/// lives in the adapter because every check speaks in Extent/DOM vocabulary.
/// </summary>
public sealed class ReportArtifactInspector
{
    public static ReportArtifactInspection Inspect(string reportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        var fullPath = Path.GetFullPath(reportPath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("The report artifact does not exist.", fullPath);

        var html = File.ReadAllText(fullPath);
        var reportDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty;
        var checks = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["custom-theme"] = html.Contains("id=\"atf-custom-theme\"", StringComparison.Ordinal),
            ["dark-token"] = html.Contains("--atf-dark:", StringComparison.Ordinal) && html.Contains("--atf-bg: var(--atf-dark)", StringComparison.Ordinal),
            ["self-contained"] = !ContainsExternalExtentReference(html),
            ["extent-folder-removed"] = !Directory.Exists(Path.Combine(reportDirectory, "extent")),
            ["collapsed-evidence"] = HasCollapsedDetails(html),
            ["scrollable-json"] = HasScrollableJsonBlock(html),
            ["assertion-table"] = HasAssertionTable(html),
            ["assertion-table-headers"] = HasAssertionTableHeaders(html),
            ["page-horizontal-overflow-guard"] = HasPageHorizontalOverflowGuard(html),
            ["technical-trace"] = html.Contains("TECHNICAL TRACE", StringComparison.Ordinal),
            ["response-duration-ms"] = html.Contains("RESPONSE DURATION (ms)", StringComparison.Ordinal),
            ["no-generic-framework-tag"] = !HasGenericFrameworkTag(html),
            ["accessible-evidence-titles"] = HasAccessibleEvidenceTitles(html),
            ["semantic-execution-roles"] = HasSemanticExecutionRoles(html),
            ["secondary-overview-collapsed"] = HasCollapsedSecondaryOverview(html),
            ["composite-summary-cleanup"] = html.Contains("removeCompositeScenarioSummary", StringComparison.Ordinal),
            ["phase-shells-collapsed"] = html.Contains("body.classList.remove('show')", StringComparison.Ordinal) && html.Contains("header.setAttribute('aria-expanded', 'false')", StringComparison.Ordinal),
            ["timestamp-column-hidden"] = html.Contains("timestamp-col", StringComparison.Ordinal) && html.Contains("display: none !important", StringComparison.Ordinal),
            ["inner-phase-rail-removed"] = html.Contains("border-left: 0 !important", StringComparison.Ordinal),
            ["response-owned-execution-hierarchy"] = html.Contains("configureExecutionHierarchy", StringComparison.Ordinal) && HasSemanticExecutionRoles(html),
            ["typed-failure-projection"] = HasTypedFailureProjection(html),
            ["failure-id-idempotency"] = html.Contains("if (!failureIds.includes(failureId)) failureIds.push(failureId);", StringComparison.Ordinal),
            ["dom-contract-harness"] = HasDomContractHarness(html)
        };

        return new ReportArtifactInspection(fullPath, checks);
    }

    private static bool HasCollapsedDetails(string html)
    {
        var details = Regex.Matches(html, "<details\\b(?<attributes>[^>]*)>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var stepDetails = details.Cast<Match>().Where(match => HasClass(match.Groups["attributes"].Value, "step-detail")).ToArray();
        return stepDetails.Length > 0 && stepDetails.All(match => !Regex.IsMatch(match.Groups["attributes"].Value, "(?:^|\\s)open(?:\\s|=|$)", RegexOptions.IgnoreCase));
    }

    private static bool HasScrollableJsonBlock(string html) => html.Contains("helm-scroll-block-body", StringComparison.Ordinal) &&
        (Regex.IsMatch(html, "\\.helm-scroll-block-body\\s*\\{[^}]*overflow-y\\s*:\\s*auto[^}]*overflow-x\\s*:\\s*hidden", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant) ||
         Regex.IsMatch(html, "\\.helm-scroll-block-body\\s*\\{[^}]*overflow-x\\s*:\\s*hidden[^}]*overflow-y\\s*:\\s*auto", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant));

    private static bool HasAssertionTable(string html) => Regex.IsMatch(html, "<table\\b[^>]*class\\s*=\\s*(['\"])[^'\"]*\\bhelm-validation-table\\b[^'\"]*\\1[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool HasAssertionTableHeaders(string html)
    {
        foreach (Match table in Regex.Matches(html, "<table\\b[^>]*class\\s*=\\s*(['\"])[^'\"]*\\bhelm-validation-table\\b[^'\"]*\\1[^>]*>(?<body>.*?)</table>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant))
        {
            var header = Regex.Match(table.Groups["body"].Value, "<thead\\b[^>]*>(?<head>.*?)</thead>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
            if (!header.Success) continue;
            var cells = Regex.Matches(header.Groups["head"].Value, "<th\\b[^>]*>(?<text>.*?)</th>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant).Cast<Match>().Select(cell => NormalizeCellText(cell.Groups["text"].Value)).ToArray();
            if (cells.SequenceEqual(["ASSERTION", "EXPECTED", "ACTUAL", "RESULT"], StringComparer.Ordinal)) return true;
        }
        return false;
    }

    private static bool HasPageHorizontalOverflowGuard(string html) => Regex.IsMatch(html, "(?:html\\s*,\\s*body|\\.test-wrapper[^{}]*\\.test-content[^{}]*\\.test-content-detail)[^{]*\\{[^}]*overflow-x\\s*:\\s*hidden", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static bool HasGenericFrameworkTag(string html) => Regex.IsMatch(html, "(?:tag\\s*=\\s*(['\"])[^'\"]*\\bframework\\b[^'\"]*\\1|<a\\b[^>]*class\\s*=\\s*(['\"])[^'\"]*\\bdropdown-item\\b[^'\"]*\\2[^>]*>\\s*framework\\s*</a>|<[^>]*class\\s*=\\s*(['\"])[^'\"]*\\bbadge\\b[^'\"]*\\3[^>]*>\\s*(?:<[^>]+>\\s*)*framework\\s*(?:<[^>]+>\\s*)*</[^>]+>)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static bool HasAccessibleEvidenceTitles(string html) => Regex.IsMatch(html, "<span\\b[^>]*class\\s*=\\s*(['\"])[^'\"]*\\bse-title\\b[^'\"]*\\1[^>]*title\\s*=\\s*(['\"])[^'\"]*\\2[^>]*aria-label\\s*=\\s*(['\"])[^'\"]*\\3", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static bool HasSemanticExecutionRoles(string html)
    {
        static bool HasRole(string value, string role) => Regex.IsMatch(value, $"data-atf-node-role\\s*=\\s*(['\"]){Regex.Escape(role)}\\1", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var materialized = HasRole(html, "phase") && HasRole(html, "request") && HasRole(html, "validation") && Regex.IsMatch(html, "data-atf-parent-node\\s*=\\s*(['\"])phase\\1", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) && Regex.IsMatch(html, "data-atf-node-role\\s*=\\s*(['\"])validation\\1[^>]*data-atf-parent-node\\s*=\\s*(['\"])phase\\2", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var client = html.Contains("configureExecutionHierarchy", StringComparison.Ordinal) && html.Contains("phaseCard.setAttribute('data-atf-node-role', 'phase')", StringComparison.Ordinal) && html.Contains("stepCard.setAttribute('data-atf-node-role', 'request')", StringComparison.Ordinal) && html.Contains("stepCard.setAttribute('data-atf-parent-node', 'phase')", StringComparison.Ordinal) && html.Contains("validationCard.setAttribute('data-atf-node-role', 'validation')", StringComparison.Ordinal) && html.Contains("validationCard.setAttribute('data-atf-parent-node', 'phase')", StringComparison.Ordinal);
        return materialized || client;
    }

    private static bool HasCollapsedSecondaryOverview(string html)
    {
        var disclosures = Regex.Matches(html, "<details\\b(?<attributes>[^>]*)>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Cast<Match>().Where(match => HasClass(match.Groups["attributes"].Value, "atf-secondary-overview")).ToArray();
        var materialized = disclosures.Length >= 2 && disclosures.All(match => !Regex.IsMatch(match.Groups["attributes"].Value, "(?:^|\\s)open(?:\\s|=|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
        var client = html.Contains("className = 'atf-secondary-overview'", StringComparison.Ordinal) && html.Contains("header.setAttribute('aria-expanded', 'false')", StringComparison.Ordinal);
        return materialized || client;
    }

    private static bool HasTypedFailureProjection(string html) =>
        html.Contains("class FailureRecord", StringComparison.Ordinal)
        && html.Contains("class FailureIndex", StringComparison.Ordinal)
        && html.Contains("function collectFailureCandidates()", StringComparison.Ordinal)
        && html.Contains("function projectFailureRecords(candidates)", StringComparison.Ordinal)
        && html.Contains("return new FailureIndex(records)", StringComparison.Ordinal);

    private static bool HasDomContractHarness(string html) =>
        html.Contains("window.__atfDomContract", StringComparison.Ordinal)
        && html.Contains("version: 1", StringComparison.Ordinal)
        && html.Contains("inspect: function ()", StringComparison.Ordinal)
        && html.Contains("statusBesideIdentity", StringComparison.Ordinal)
        && html.Contains("openEvidenceCount", StringComparison.Ordinal)
        && html.Contains("overflowY", StringComparison.Ordinal);

    private static bool HasClass(string attributes, string className) => Regex.IsMatch(attributes, $"\\bclass\\s*=\\s*(['\"]).*?\\b{Regex.Escape(className)}\\b.*?\\1", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static string NormalizeCellText(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<.*?>", string.Empty)), "\\s+", " ").Trim().ToUpperInvariant();
    private static bool ContainsExternalExtentReference(string html) => html.Contains("href=\"extent/", StringComparison.OrdinalIgnoreCase) || html.Contains("href='extent/", StringComparison.OrdinalIgnoreCase) || html.Contains("src=\"extent/", StringComparison.OrdinalIgnoreCase) || html.Contains("src='extent/", StringComparison.OrdinalIgnoreCase) || html.Contains("url(extent/", StringComparison.OrdinalIgnoreCase) || html.Contains("url('extent/", StringComparison.OrdinalIgnoreCase) || html.Contains("url(\"extent/", StringComparison.OrdinalIgnoreCase);
}

public sealed class ReportArtifactInspection
{
    private readonly IReadOnlyDictionary<string, bool> _checks;
    internal ReportArtifactInspection(string reportPath, IReadOnlyDictionary<string, bool> checks) { ReportPath = reportPath; _checks = checks; }
    public string ReportPath { get; }
    public bool IsCompliant => _checks.Values.All(value => value);
    public IReadOnlyDictionary<string, bool> Checks => _checks;
    public IReadOnlyList<string> Violations => _checks.Where(pair => !pair.Value).Select(pair => pair.Key).ToArray();
    public bool Satisfies(string checkName) => _checks.TryGetValue(checkName, out var passed) && passed;
}
