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
        Assert.Equal(AtfReportStatus.Fail, row.ResultStatus);
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

    [Fact(DisplayName = "ValidationTable converts predicate exceptions into one failed assertion")]
    public void PredicateExceptionIsPublishedAsFailure()
    {
        var reporter = new RecordingReporter();

        var exception = Assert.Throws<InvalidOperationException>(() => ValidationTable.Run(
            reporter,
            "Validate: predicate failure",
            table => table.Check("schema", "valid", "invalid", () => throw new InvalidOperationException("schema unavailable"))));

        Assert.Equal("schema unavailable", exception.Message);
        var row = Assert.Single(reporter.Tables.Single());
        Assert.Equal("schema", row.Name);
        Assert.False(row.Passed);
        Assert.Contains("InvalidOperationException", row.Actual, StringComparison.Ordinal);
        Assert.Equal([AtfReportStatus.Fail], reporter.CompletedStatuses);
    }

    [Fact(DisplayName = "Soft validation keeps failed rows and publishes one warning without throwing")]
    public void SoftValidationPublishesWarningWithoutThrowing()
    {
        var reporter = new RecordingReporter();

        var exception = Record.Exception(() => ValidationTable.RunWarning(
            reporter,
            "Validate: soft contract warning",
            table => table.Add("HTTP status", "201 Created", "200 OK", passed: false)));

        Assert.Null(exception);
        var row = Assert.Single(reporter.Tables.Single());
        Assert.False(row.Passed);
        Assert.Equal(AtfReportStatus.Warning, row.ResultStatus);
        Assert.Equal([AtfReportStatus.Warning], reporter.CompletedStatuses);
        Assert.Contains("Soft validation warning", reporter.Warnings.Single(), StringComparison.Ordinal);
        Assert.Empty(reporter.Failures);
        Assert.False(reporter.LastPassCalled);
    }

    [Fact(DisplayName = "ValidationTable always closes the reporter step after a configuration failure")]
    public void ConfigurationFailureStillCompletesStep()
    {
        var reporter = new RecordingReporter();

        Assert.Throws<InvalidOperationException>(() => ValidationTable.Run(
            reporter,
            "Validate: configuration failure",
            _ => throw new InvalidOperationException("configuration failed")));

        Assert.Equal([AtfReportStatus.Fail], reporter.CompletedStatuses);
        Assert.Single(reporter.Tables);
        Assert.False(reporter.LastPassCalled);
    }

    [Fact(DisplayName = "Validation result remains authoritative when every reporter hook fails")]
    public void ReporterFailuresDoNotReplacePassingValidation()
    {
        var reporter = new ReporterThatFailsEveryPublication();

        var exception = Record.Exception(() => ValidationTable.Run(
            reporter,
            "Validate: reporter resilience",
            table => table.Add("HTTP status", "200", "200", passed: true)));

        Assert.Null(exception);
        Assert.Equal([AtfReportStatus.Pass], reporter.CompletedStatuses);
    }

    [Fact(DisplayName = "Validation failure remains authoritative when the reporter fails")]
    public void ReporterFailuresDoNotReplaceValidationFailure()
    {
        var reporter = new ReporterThatFailsEveryPublication();

        var exception = Assert.Throws<Atf.Common.Errors.ApiValidationException>(() => ValidationTable.Run(
            reporter,
            "Validate: functional failure",
            table => table.Add("HTTP status", "201", "200", passed: false)));

        Assert.Contains("HTTP status", exception.Rule, StringComparison.Ordinal);
        Assert.Equal([AtfReportStatus.Fail], reporter.CompletedStatuses);
    }

    [Fact(DisplayName = "Validation configuration exception wins over reporter failure")]
    public void ConfigurationExceptionWinsOverReporterFailure()
    {
        var reporter = new ReporterThatFailsEveryPublication();
        var expected = new InvalidOperationException("configuration is invalid");

        var exception = Assert.Throws<InvalidOperationException>(() => ValidationTable.Run(
            reporter,
            "Validate: configuration failure",
            _ => throw expected));

        Assert.Same(expected, exception);
        Assert.Equal([AtfReportStatus.Fail], reporter.CompletedStatuses);
    }

    private sealed class RecordingReporter : IAtfReporter
    {
        public List<IReadOnlyCollection<AtfAssertion>> Tables { get; } = [];
        public List<AtfReportStatus> CompletedStatuses { get; } = [];
        public bool LastPassCalled { get; private set; }
        public List<string> Warnings { get; } = [];
        public List<string> Failures { get; } = [];

        public void StartStep(string title) { }
        public void CompleteStep(AtfReportStatus status) => CompletedStatuses.Add(status);
        public void Detail(string key, string value) { }
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) => Tables.Add(assertions);
        public void Info(string message) { }
        public void Pass(string message) => LastPassCalled = true;
        public void Fail(string message, Exception? exception = null) => Failures.Add(message);
        public void Warning(string message) => Warnings.Add(message);
        public void Skip(string reason) { }
    }

    private sealed class ReporterThatFailsEveryPublication : IAtfReporter
    {
        public List<AtfReportStatus> CompletedStatuses { get; } = [];

        public int AssertionMaxDisplayCharacters => throw new InvalidOperationException("limit publication failed");
        public void StartStep(string title) => throw new InvalidOperationException("start publication failed");
        public void CompleteStep(AtfReportStatus status)
        {
            CompletedStatuses.Add(status);
            throw new InvalidOperationException("complete publication failed");
        }
        public void Detail(string key, string value) => throw new InvalidOperationException("detail publication failed");
        public void Json(string label, string json) => throw new InvalidOperationException("json publication failed");
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) => throw new InvalidOperationException("table publication failed");
        public void Info(string message) => throw new InvalidOperationException("info publication failed");
        public void Pass(string message) => throw new InvalidOperationException("pass publication failed");
        public void Fail(string message, Exception? exception = null) => throw new InvalidOperationException("fail publication failed");
        public void Warning(string message) => throw new InvalidOperationException("warning publication failed");
        public void Skip(string reason) => throw new InvalidOperationException("skip publication failed");
    }
}
