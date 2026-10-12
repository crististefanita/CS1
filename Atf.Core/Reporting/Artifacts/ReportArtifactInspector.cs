namespace Atf.Core.Reporting.Artifacts;

/// <summary>
/// Executable contract for the final report artifact. This is intentionally
/// independent from Extent's object model: it verifies the file that a user
/// opens, not an intermediate reporter state.
/// </summary>
public sealed class ReportArtifactInspector
{
    public ReportArtifactInspection Inspect(string reportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        var fullPath = Path.GetFullPath(reportPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The report artifact does not exist.", fullPath);
        }

        var html = File.ReadAllText(fullPath);
        var reportDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty;
        var externalExtentReference = ContainsExternalExtentReference(html);

        var checks = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["custom-theme"] = html.Contains("id=\"atf-custom-theme\"", StringComparison.Ordinal),
            ["dark-token"] = html.Contains("--atf-dark:", StringComparison.Ordinal)
                && html.Contains("--atf-bg: var(--atf-dark)", StringComparison.Ordinal),
            ["self-contained"] = !externalExtentReference,
            ["extent-folder-removed"] = !Directory.Exists(Path.Combine(reportDirectory, "extent")),
            ["collapsed-evidence"] = html.Contains("details class='step-detail'", StringComparison.Ordinal)
                && !html.Contains("details class='step-detail' open", StringComparison.Ordinal),
            ["scrollable-json"] = html.Contains("helm-scroll-block-body", StringComparison.Ordinal),
            ["assertion-table"] = html.Contains("helm-validation-table", StringComparison.Ordinal),
            ["technical-trace"] = html.Contains("TECHNICAL TRACE", StringComparison.Ordinal),
            ["composite-summary-cleanup"] = html.Contains("removeCompositeScenarioSummary", StringComparison.Ordinal),
            ["phase-shells-collapsed"] = html.Contains("body.classList.remove('show')", StringComparison.Ordinal)
                && html.Contains("header.setAttribute('aria-expanded', 'false')", StringComparison.Ordinal),
            ["timestamp-column-hidden"] = html.Contains("timestamp-col", StringComparison.Ordinal)
                && html.Contains("display: none !important", StringComparison.Ordinal),
            ["inner-phase-rail-removed"] = html.Contains("border-left: 0 !important", StringComparison.Ordinal),
            ["response-owned-execution-hierarchy"] = html.Contains("configureExecutionHierarchy", StringComparison.Ordinal)
                && html.Contains("data-atf-node-role=\"request\"", StringComparison.Ordinal)
                && html.Contains("data-atf-node-role=\"validation\"", StringComparison.Ordinal)
                && html.Contains("data-atf-parent-node', 'request", StringComparison.Ordinal)
        };

        return new ReportArtifactInspection(fullPath, checks);
    }

    private static bool ContainsExternalExtentReference(string html) =>
        html.Contains("href=\"extent/", StringComparison.OrdinalIgnoreCase)
        || html.Contains("href='extent/", StringComparison.OrdinalIgnoreCase)
        || html.Contains("src=\"extent/", StringComparison.OrdinalIgnoreCase)
        || html.Contains("src='extent/", StringComparison.OrdinalIgnoreCase)
        || html.Contains("url(extent/", StringComparison.OrdinalIgnoreCase)
        || html.Contains("url('extent/", StringComparison.OrdinalIgnoreCase)
        || html.Contains("url(\"extent/", StringComparison.OrdinalIgnoreCase);
}

public sealed class ReportArtifactInspection
{
    private readonly IReadOnlyDictionary<string, bool> _checks;

    internal ReportArtifactInspection(string reportPath, IReadOnlyDictionary<string, bool> checks)
    {
        ReportPath = reportPath;
        _checks = checks;
    }

    public string ReportPath { get; }
    public bool IsCompliant => _checks.Values.All(value => value);
    public IReadOnlyDictionary<string, bool> Checks => _checks;
    public IReadOnlyList<string> Violations => _checks
        .Where(pair => !pair.Value)
        .Select(pair => pair.Key)
        .ToArray();

    public bool Satisfies(string checkName) =>
        _checks.TryGetValue(checkName, out var passed) && passed;
}
