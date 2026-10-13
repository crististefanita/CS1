using Atf.Common.Errors;
using Atf.Common.Validation.Execution;
using Atf.Core.Reporting;
using System.Collections.ObjectModel;

namespace Atf.Common.Validation;

/// <summary>
/// Collects all assertions belonging to one API contract before publishing
/// one Extent evidence table. This mirrors the source ATF's ValidationEvidence
/// model while keeping the runner independent of a particular test framework.
/// </summary>
public sealed class ValidationTable
{
    public const int DefaultMaxDisplayedCharacters = ReportingDefaults.AssertionMaxDisplayCharacters;

    private readonly List<AtfAssertion> _rows = [];
    private readonly List<Exception> _failures = [];
    private readonly HashSet<string> _assertionNames = new(StringComparer.OrdinalIgnoreCase);

    public ValidationTable(int maxDisplayedCharacters = DefaultMaxDisplayedCharacters)
    {
        if (maxDisplayedCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDisplayedCharacters), "The assertion display limit must be positive.");
        }

        MaxDisplayedCharacters = maxDisplayedCharacters;
    }

    public IReadOnlyList<AtfAssertion> Rows => new ReadOnlyCollection<AtfAssertion>(_rows);
    public bool HasFailures => _rows.Any(row => !row.Passed);
    public bool HasAssertions => _rows.Count > 0;
    public bool IsFinished { get; private set; }
    public int MaxDisplayedCharacters { get; }

    public string Preview(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Length <= MaxDisplayedCharacters
            ? value
            : value[..Math.Max(0, MaxDisplayedCharacters - 1)] + "…";
    }

    public void Check(
        string name,
        string expected,
        string actual,
        Func<bool> predicate)
    {
        try
        {
            var passed = predicate();
            Add(name, expected, actual, passed);
        }
        catch (Exception exception)
        {
            AddFailure(name, expected, actual, exception);
        }
    }

    public void Add(string name, string expected, string actual, bool passed)
    {
        EnsureWritable();
        AddIdentity(name);
        _rows.Add(new AtfAssertion(name, expected, actual, passed));
        if (!passed)
        {
            _failures.Add(new ApiValidationException(name, expected, actual));
        }
    }

    public void AddFailure(string name, string expected, string actual, Exception exception)
    {
        EnsureWritable();
        AddIdentity(name);
        _rows.Add(new AtfAssertion(name, expected, $"{actual}\n{exception.GetType().Name}: {exception.Message}", false));
        _failures.Add(exception);
    }

    internal Exception FirstFailure(string fallbackRule = "validation")
    {
        return _failures.FirstOrDefault()
            ?? new ApiValidationException(fallbackRule, "all assertions pass", "one or more assertions failed");
    }

    public static void Run(
        IAtfValidationReporter reporter,
        string title,
        Action<ValidationTable> configure)
        => new ValidationTableRunner(reporter).Run(title, configure);

    /// <summary>
    /// Publishes all assertion rows but treats failed rows as a non-blocking
    /// warning. The failed rows remain visible in the aggregate table and the
    /// scenario is allowed to surface as Warning/Other in the report overview.
    /// Unexpected configuration exceptions still fail fast.
    /// </summary>
    public static void RunWarning(
        IAtfValidationReporter reporter,
        string title,
        Action<ValidationTable> configure)
        => new ValidationTableRunner(reporter).RunWarning(title, configure);

    internal void MarkFinished() => IsFinished = true;

    internal void AddMissingAssertionRow()
    {
        EnsureWritable();
        AddIdentity("Validation assertions");
        _rows.Add(new AtfAssertion(
            "Validation assertions",
            "at least one assertion",
            "0 assertions registered",
            Passed: false,
            ResultStatus: AtfReportStatus.Fail));
        _failures.Add(new ApiValidationException(
            "Validation assertions",
            "at least one assertion",
            "0 assertions registered"));
    }

    private void EnsureWritable()
    {
        if (IsFinished)
        {
            throw new InvalidOperationException("A ValidationTable cannot be changed after it is published.");
        }
    }

    private void AddIdentity(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!_assertionNames.Add(name.Trim()))
        {
            throw new InvalidOperationException($"Duplicate assertion '{name}' is not allowed in one ValidationTable.");
        }
    }
}
