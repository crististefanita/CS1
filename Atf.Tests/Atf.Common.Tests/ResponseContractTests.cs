using Atf.Common.Validation;
using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;

namespace Atf.Common.Tests;

public sealed class ResponseContractTests
{
    [Fact(DisplayName = "One response contract publishes one aggregate table with ordered rules")]
    public void ContractAggregatesRulesOnce()
    {
        var reporter = new RecordingReporter();
        var response = Response("{\"id\":42,\"status\":\"active\"}");
        var contract = new ResponseContract()
            .ExpectStatus(HttpStatusCode.OK)
            .ExpectContentType("application/json")
            .ExpectJsonPath("$.id")
            .ExpectJsonPath("$.status", "active");

        ApiAssertions.Contract(reporter, response, "Validate: user contract", contract);

        var rows = Assert.Single(reporter.Tables);
        Assert.Equal(4, rows.Count);
        Assert.Equal(
            ["HTTP status", "Content-Type", "JSONPath exists: $.id", "JSONPath equals: $.status"],
            rows.Select(row => row.Name));
        Assert.All(rows, row => Assert.True(row.Passed));
        Assert.Equal(["start:Validate: user contract", "complete:Pass"], reporter.StepEvents);
    }

    [Fact(DisplayName = "Response contract cannot be evaluated twice")]
    public void ContractIsSingleUse()
    {
        var contract = new ResponseContract().ExpectStatus(HttpStatusCode.OK);
        var table = new ValidationTable();
        contract.Evaluate(Response("{}"), table);

        Assert.Throws<InvalidOperationException>(() => contract.Evaluate(Response("{}"), new ValidationTable()));
        Assert.Throws<InvalidOperationException>(() => contract.ExpectJsonPath("$.id"));
    }

    private static ApiResponse Response(string body) => new(
        HttpStatusCode.OK,
        body,
        new Dictionary<string, string> { ["Content-Type"] = "application/json; charset=utf-8" },
        TimeSpan.Zero,
        "https://example.test/users/42");

    private sealed class RecordingReporter : IAtfReporter
    {
        public List<IReadOnlyCollection<AtfAssertion>> Tables { get; } = [];
        public List<string> StepEvents { get; } = [];

        public void StartStep(string title) => StepEvents.Add($"start:{title}");
        public void CompleteStep(AtfReportStatus status) => StepEvents.Add($"complete:{status}");
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
