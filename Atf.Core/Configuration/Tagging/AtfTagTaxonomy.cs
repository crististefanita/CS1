namespace Atf.Core.Configuration.Tagging;

/// <summary>
/// Renderer-neutral policy for classifying and normalizing scenario metadata.
/// Unknown tags are deterministic diagnostic metadata until explicitly added to
/// TagTaxonomySettings.SelectionTagNames.
/// </summary>
public sealed class AtfTagTaxonomy
{
    private readonly HashSet<string> _selection;
    private readonly HashSet<string> _diagnostic;

    public AtfTagTaxonomy(IEnumerable<string>? selectionTagNames, IEnumerable<string>? diagnosticTagNames)
    {
        _selection = Normalize(selectionTagNames).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _diagnostic = Normalize(diagnosticTagNames).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _selection.ExceptWith(_diagnostic);
    }

    public IReadOnlyCollection<string> SelectionTagNames => _selection;

    public IReadOnlyCollection<string> DiagnosticTagNames => _diagnostic;

    public AtfTagKind Classify(string tag)
    {
        var normalized = Normalize(tag);
        return normalized.Length > 0 && _selection.Contains(normalized)
            ? AtfTagKind.Selection
            : AtfTagKind.Diagnostic;
    }

    public static IReadOnlyList<string> NormalizeAndDistinct(IEnumerable<string>? tags)
        => Normalize(tags).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<string> SelectableTags(IEnumerable<string>? tags)
        => NormalizeAndDistinct(tags).Where(tag => Classify(tag) == AtfTagKind.Selection).ToArray();

    public IReadOnlyList<string> DiagnosticTags(IEnumerable<string>? tags)
        => NormalizeAndDistinct(tags).Where(tag => Classify(tag) == AtfTagKind.Diagnostic).ToArray();

    private static IEnumerable<string> Normalize(IEnumerable<string>? tags)
        => (tags ?? []).Select(Normalize).Where(static tag => tag.Length > 0);

    private static string Normalize(string? tag) => tag?.Trim() ?? string.Empty;
}
