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
    }

    private sealed class RecordingReporter : IAtfReporter
    {
        public List<IReadOnlyCollection<AtfAssertion>> Tables { get; } = [];
        public List<string> Failures { get; } = [];
        public List<string> Passes { get; } = [];
        public List<string> Warnings { get; } = [];

        public void StartStep(string title) { }
        public void Detail(string key, string value) { }
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) => Tables.Add(assertions);
        public void Info(string message) { }
        public void Pass(string message) => Passes.Add(message);
        public void Fail(string message, Exception? exception = null) => Failures.Add(message);
        public void Warning(string message) => Warnings.Add(message);
        public void Skip(string reason) { }
    }
}
