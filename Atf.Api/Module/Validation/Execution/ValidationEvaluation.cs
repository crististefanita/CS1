using Atf.Core.Reporting;

namespace Atf.Common.Validation.Execution;

/// <summary>
/// Renderer-neutral result of evaluating one response contract. It contains
/// only immutable assertion evidence and exception policy inputs; it cannot
/// publish Extent or choose an HTML representation.
/// </summary>
internal sealed record ValidationEvaluation(
    IReadOnlyList<AtfAssertion> Rows,
    bool HasFailures,
    Exception? Failure,
    Exception? UnexpectedException)
{
    public bool IsUnexpectedFailure => UnexpectedException is not null;
}
