namespace Atf.Core.Reporting.Evidence;

/// <summary>
/// One row in the aggregated validation evidence rendered by the reporter.
/// The row is deliberately framework-neutral so a future config-driven runner
/// can create the same table from YAML/JSON without depending on xUnit.
/// </summary>
public sealed record AtfAssertion(
    string Name,
    string Expected,
    string Actual,
    bool Passed);
