using Atf.Common.Validation;
using Atf.Core.Reporting;

namespace Atf.Common.Tests;

public sealed class ValidationTableContractTests
{
    [Fact(DisplayName = "ValidationTable rejects duplicate assertion identities")]
    public void DuplicateAssertionIsRejected()
    {
        var table = new ValidationTable();

        table.Add("HTTP status", "200", "200", true);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            table.Add("HTTP status", "200", "200", true));

        Assert.Contains("Duplicate assertion", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Empty ValidationTable has an explicit failed contract row")]
    public void EmptyTableIsExplicitlyInvalid()
    {
        var reporter = new RecordingReporter();

        Assert.Throws<Atf.Common.Errors.ApiValidationException>(() => ValidationTable.Run(
            reporter,
            "Validate: empty contract",
            _ => { }));

        var row = Assert.Single(reporter.Tables.Single());
        Assert.Equal("Validation assertions", row.Name);
        Assert.False(row.Passed);
        Assert.Contains("at least one assertion", row.Expected, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "ValidationTable keeps the full actual value while rendering may truncate it")]
    public void ActualValueIsNotTruncatedInTheModel()
    {
        var reporter = new RecordingReporter();
        var actual = new string('x', 400);

        ValidationTable.Run(reporter, "Validate: long value", table =>
            table.Add("payload", "expected", actual, true));

        Assert.Equal(actual, Assert.Single(reporter.Tables.Single()).Actual);
    }

    [Fact(DisplayName = "Published ValidationTable cannot be mutated")]
    public void PublishedTableIsImmutable()
    {
        var reporter = new RecordingReporter();
        ValidationTable? table = null;

        ValidationTable.Run(reporter, "Validate: immutable", current =>
        {
            table = current;
            current.Add("status", "200", "200", true);
        });

        Assert.True(table!.IsFinished);
        Assert.Throws<InvalidOperationException>(() => table.Add("late", "x", "x", true));
    }

    private sealed class RecordingReporter : IAtfReporter
    {
        public List<IReadOnlyCollection<AtfAssertion>> Tables { get; } = [];

        public void StartStep(string title) { }
        public void CompleteStep(AtfReportStatus status) { }
        public void Detail(string key, string value) { }
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) => Tables.Add(assertions);
        public void Info(string message) { }
        public void Pass(string message) { }
        public void Fail(string message, Exception? exception = null) { }
        public void Warning(string message) { }
        public void Skip(string reason) { }
    }
}
