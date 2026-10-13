using Atf.Common.Errors;
using Atf.Common.Lifecycle;
using Atf.Common.Validation;
using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;

namespace Atf.Common.Tests;

public sealed class ValidationAndLifecycleTests
{
    [Fact(DisplayName = "Validation table aggregates all rows before failing once")]
    public void ValidationTableAggregatesRows()
    {
        var reporter = new RecordingReporter();
        var response = new ApiResponse(
            HttpStatusCode.OK,
            "{\"id\":1}",
            new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            TimeSpan.Zero,
            "https://example.test/users/1");

        var exception = Assert.Throws<ApiValidationException>(() =>
            ValidationTable.Run(reporter, "Validate contract", table =>
            {
                table.Check("HTTP status", "200 OK", "200 OK", () => true);
                table.Check("JSON id", "2", "1", () => false);
                table.Check("Content-Type", "application/json", "application/json", () => true);
            }));

        Assert.Equal("JSON id", exception.Rule);
        Assert.Single(reporter.Tables);
        Assert.Equal(3, reporter.Tables[0].Count);
        Assert.Contains("Validation failed.", reporter.Failures);
        Assert.Empty(reporter.Passes);
        Assert.Equal(["start:Validate contract", "complete:Fail"], reporter.StepEvents);
    }

    [Fact(DisplayName = "Passing validation closes its Extent step before the next validator")]
    public void PassingValidationClosesItsStep()
    {
        var reporter = new RecordingReporter();

        ValidationTable.Run(reporter, "Validate: status=200", table =>
        {
            table.Check("HTTP status", "200 OK", "200 OK", () => true);
        });

        ValidationTable.Run(reporter, "Validate: content-type", table =>
        {
            table.Check("Content-Type", "application/json", "application/json", () => true);
        });

        Assert.Equal(
            [
                "start:Validate: status=200",
                "complete:Pass",
                "start:Validate: content-type",
                "complete:Pass"
            ],
            reporter.StepEvents);
    }

    [Fact(DisplayName = "Unexpected validator exceptions close the step as failed")]
    public void UnexpectedValidatorExceptionClosesStepAsFailed()
    {
        var reporter = new RecordingReporter();

        Assert.Throws<InvalidOperationException>(() => ValidationTable.Run(
            reporter,
            "Validate: unexpected",
            _ => throw new InvalidOperationException("validator exploded")));

        Assert.Equal(["start:Validate: unexpected", "complete:Fail"], reporter.StepEvents);
        Assert.Single(reporter.Tables);
        Assert.False(Assert.Single(reporter.Tables[0]).Passed);
    }

    [Fact(DisplayName = "Cleanup registry runs owned resources in reverse order and is reusable")]
    public async Task CleanupRegistryRunsInReverseOrder()
    {
        var reporter = new RecordingReporter();
        var registry = new CleanupRegistry(reporter);
        var order = new List<string>();
        registry.Own("user", "1", () => { order.Add("user"); return Task.CompletedTask; });
        registry.Own("post", "2", () => { order.Add("post"); return Task.CompletedTask; });

        await registry.RunAsync();

        Assert.Equal(["post", "user"], order);
        Assert.False(registry.HasPending);
        Assert.Equal(2, reporter.Passes.Count);
        Assert.Equal(
            ["start:Cleanup: DELETE post/2", "complete:Pass", "start:Cleanup: DELETE user/1", "complete:Pass"],
            reporter.StepEvents);
    }

    [Fact(DisplayName = "Cleanup registry keeps cleanup best-effort when one action fails")]
    public async Task CleanupRegistryContinuesAfterFailure()
    {
        var reporter = new RecordingReporter();
        var registry = new CleanupRegistry(reporter);
        var order = new List<string>();
        registry.Own("user", "1", () => throw new InvalidOperationException("blocked"));
        registry.Own("post", "2", () => { order.Add("post"); return Task.CompletedTask; });

        await registry.RunAsync();

        Assert.Equal(["post"], order);
        Assert.Contains(reporter.Warnings, warning => warning.Contains("blocked", StringComparison.Ordinal));
        Assert.False(registry.HasPending);
        Assert.Contains("complete:Warning", reporter.StepEvents);
    }

    [Fact(DisplayName = "Cleanup registry is idempotent after the first run")]
    public async Task CleanupRegistryDoesNotRunTwice()
    {
        var reporter = new RecordingReporter();
        var registry = new CleanupRegistry(reporter);
        var calls = 0;
        registry.Own("user", "1", () => { calls++; return Task.CompletedTask; });

        await registry.RunAsync();
        await registry.RunAsync();

        Assert.Equal(1, calls);
        Assert.Single(reporter.Passes);
    }

    [Fact(DisplayName = "Cleanup registries remain isolated between scenarios")]
    public async Task CleanupRegistriesAreIsolated()
    {
        var firstReporter = new RecordingReporter();
        var secondReporter = new RecordingReporter();
        var first = new CleanupRegistry(firstReporter);
        var second = new CleanupRegistry(secondReporter);
        var firstCalls = 0;
        var secondCalls = 0;

        first.Own("user", "first", () => { firstCalls++; return Task.CompletedTask; });
        second.Own("user", "second", () => { secondCalls++; return Task.CompletedTask; });

        await first.RunAsync();

        Assert.Equal(1, firstCalls);
        Assert.Equal(0, secondCalls);
        Assert.Single(firstReporter.Passes);
        Assert.Empty(secondReporter.Passes);
        Assert.False(first.HasPending);
        Assert.True(second.HasPending);
    }

    [Fact(DisplayName = "Cleanup registry runs actions when every reporting hook fails")]
    public async Task CleanupActionsDoNotDependOnReporterHealth()
    {
        var reporter = new ThrowingReporter();
        var registry = new CleanupRegistry(reporter);
        var order = new List<string>();
        registry.Own("user", "1", () => { order.Add("user"); return Task.CompletedTask; });
        registry.Own("post", "2", () =>
        {
            order.Add("post");
            throw new InvalidOperationException("resource delete failed");
        });

        await registry.RunAsync();

        Assert.Equal(["post", "user"], order);
        Assert.False(registry.HasPending);
    }

    private sealed class RecordingReporter : IAtfReporter
    {
        public List<IReadOnlyCollection<AtfAssertion>> Tables { get; } = [];
        public List<string> Failures { get; } = [];
        public List<string> Passes { get; } = [];
        public List<string> Warnings { get; } = [];
        public List<string> StepEvents { get; } = [];

        public void StartStep(string title) => StepEvents.Add($"start:{title}");
        public void CompleteStep(AtfReportStatus status) => StepEvents.Add($"complete:{status}");
        public void Detail(string key, string value) { }
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) => Tables.Add(assertions);
        public void Info(string message) { }
        public void Pass(string message) => Passes.Add(message);
        public void Fail(string message, Exception? exception = null) => Failures.Add(message);
        public void Warning(string message) => Warnings.Add(message);
        public void Skip(string reason) { }
    }

    private sealed class ThrowingReporter : IAtfReporter
    {
        public void StartStep(string title) => throw new InvalidOperationException("start reporting failed");
        public void CompleteStep(AtfReportStatus status) => throw new InvalidOperationException("complete reporting failed");
        public void Detail(string key, string value) { }
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) { }
        public void Info(string message) { }
        public void Pass(string message) => throw new InvalidOperationException("pass reporting failed");
        public void Fail(string message, Exception? exception = null) { }
        public void Warning(string message) => throw new InvalidOperationException("warning reporting failed");
        public void Skip(string reason) { }
    }
}
