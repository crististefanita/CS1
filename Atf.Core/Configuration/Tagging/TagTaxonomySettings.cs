namespace Atf.Core.Configuration.Tagging;

/// <summary>
/// Explicit tag taxonomy used by reporting adapters. TagOrder remains an
/// ordering rule; it does not decide eligibility.
/// </summary>
public sealed class TagTaxonomySettings
{
    public string[] SelectionTagNames { get; init; } = [];
    public string[] DiagnosticTagNames { get; init; } = [];

    public TagTaxonomySettings Snapshot() => new()
    {
        SelectionTagNames = SelectionTagNames.ToArray(),
        DiagnosticTagNames = DiagnosticTagNames.ToArray()
    };

    public AtfTagTaxonomy CreatePolicy() => new(SelectionTagNames, DiagnosticTagNames);
}
