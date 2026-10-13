namespace Atf.Reporting.Extent;

/// <summary>
/// Async-flow aware stack for one scenario's active Extent nodes. Keeping the
/// stack mechanics outside the scope makes concurrency and ownership rules
/// explicit without leaking AsyncLocal details into evidence code.
/// </summary>
internal sealed class ExtentScopeFrameStack
{
    private readonly AsyncLocal<Stack<ExtentTestScope.StepFrame>?> _frames = new();

    public ExtentTestScope.StepFrame? Current =>
        _frames.Value is { Count: > 0 } frames ? frames.Peek() : null;

    public int Count => _frames.Value?.Count ?? 0;

    public void Push(ExtentTestScope.StepFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        (_frames.Value ??= new Stack<ExtentTestScope.StepFrame>()).Push(frame);
    }

    public ExtentTestScope.StepFrame? Pop()
    {
        var frames = _frames.Value;
        if (frames is null || frames.Count == 0)
        {
            return null;
        }

        return frames.Pop();
    }

    public ExtentTestScope.StepFrame? Find(Func<ExtentTestScope.StepFrame, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _frames.Value?.FirstOrDefault(predicate);
    }
}
