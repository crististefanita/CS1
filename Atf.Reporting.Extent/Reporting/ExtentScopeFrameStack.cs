namespace Atf.Reporting.Extent.Scope;

/// <summary>
/// Async-flow aware stack for one scenario's active Extent nodes. Keeping the
/// stack mechanics outside the scope makes concurrency and ownership rules
/// explicit without leaking AsyncLocal details into evidence code.
/// </summary>
internal sealed class ExtentScopeFrameStack
{
    private readonly AsyncLocal<Stack<ScenarioStepFrame>?> _frames = new();

    public ScenarioStepFrame? Current =>
        _frames.Value is { Count: > 0 } frames ? frames.Peek() : null;

    public int Count => _frames.Value?.Count ?? 0;

    public void Push(ScenarioStepFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        (_frames.Value ??= new Stack<ScenarioStepFrame>()).Push(frame);
    }

    public ScenarioStepFrame? Pop()
    {
        var frames = _frames.Value;
        if (frames is null || frames.Count == 0)
        {
            return null;
        }

        return frames.Pop();
    }

    public ScenarioStepFrame? Find(Func<ScenarioStepFrame, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _frames.Value?.FirstOrDefault(predicate);
    }
}
