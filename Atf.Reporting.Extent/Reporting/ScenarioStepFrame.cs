namespace Atf.Reporting.Extent.Scope;

/// <summary>
/// Renderer-neutral state for one active scenario step.
/// The frame knows only the adapter port needed to publish evidence; it does
/// not expose, own, or reference the Extent vendor model.
/// </summary>
internal sealed class ScenarioStepFrame
{
    internal ScenarioStepFrame(IExtentNodeAdapter adapter, string title)
    {
        Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
    }

    internal IExtentNodeAdapter Adapter { get; }
    internal string Title { get; }
    internal List<(string Key, string Value)> Details { get; } = [];

    internal int AssertionTableCount { get; set; }
}
