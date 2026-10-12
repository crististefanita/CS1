using Atf.Common.Errors;
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
        IAtfReporter reporter,
        string title,
        Action<ValidationTable> configure)
    {
        reporter.StartStep(title);
        var table = new ValidationTable(reporter.AssertionMaxDisplayCharacters);
        var status = AtfReportStatus.Pass;

        try
        {
            configure(table);
            table.Finish(reporter);
        }
        catch (Exception exception) when (!table.IsFinished)
        {
            status = AtfReportStatus.Fail;
            table.AddFailure("Validation execution", "no unexpected exception", exception.Message, exception);
            table.FinishUnexpected(reporter, exception);
            throw;
        }
        catch
        {
            status = AtfReportStatus.Fail;
            throw;
        }
        finally
        {
            // Every validation is a leaf evidence step. Closing it here is
            // essential when several validators follow one request: otherwise
            // Extent attaches the next validation below the previous one.
            reporter.CompleteStep(status);
        }
    }

    private void Finish(IAtfReporter reporter)
    {
        if (!HasAssertions)
        {
            Add(
                "Validation assertions",
                "at least one assertion",
                "0 assertions registered",
                passed: false);
        }

        if (HasFailures)
        {
            var failure = FirstFailure();
            IsFinished = true;
            reporter.AssertionTable(_rows);
            reporter.Fail("Validation failed.", failure);
            throw failure;
        }

        IsFinished = true;
        reporter.AssertionTable(_rows);
        reporter.Pass($"{_rows.Count} validations passed.");
    }

    private void FinishUnexpected(IAtfReporter reporter, Exception exception)
    {
        IsFinished = true;
        reporter.AssertionTable(_rows);
        reporter.Fail("Validation failed.", exception);
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
