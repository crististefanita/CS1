using Atf.Common.Errors;
using Atf.Common.Validation;
using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;

namespace Atf.Common.Tests;

public sealed class ValidationComponentTests
{
    [Fact(DisplayName = "Status assertion produces one passing evidence row")]
    public void StatusAssertionProducesPassRow()
    {
        var reporter = new RecordingReporter();

        ApiAssertions.Status(reporter, Response(HttpStatusCode.OK, "{}"), HttpStatusCode.OK);

        var row = Assert.Single(reporter.Tables.Single());
        Assert.Equal("HTTP status", row.Name);
        Assert.Equal("200 OK", row.Expected);
        Assert.True(row.Passed);
        Assert.Contains("1 validations passed.", reporter.Passes);
    }

    [Fact(DisplayName = "StatusOneOf accepts either configured negative outcome")]
    public void StatusOneOfAcceptsConfiguredAlternative()
    {
        var reporter = new RecordingReporter();

        ApiAssertions.StatusOneOf(
            reporter,
            Response(HttpStatusCode.Forbidden, "{}"),
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden);

        Assert.True(Assert.Single(reporter.Tables.Single()).Passed);
    }

    [Fact(DisplayName = "One response contract publishes one aggregate assertion table")]
    public void ResponseContractAggregatesAssertions()
    {
        var reporter = new RecordingReporter();

        ApiAssertions.Contract(
            reporter,
            Response(HttpStatusCode.OK, "{\"id\":42}", new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }),
            "Validate: aggregated response",
            contract => contract
                .Status(HttpStatusCode.OK)
                .ContentTypeContains("application/json")
                .JsonPathExists("$.id"));

        Assert.Single(reporter.Tables);
        Assert.Equal(3, reporter.Tables.Single().Count);
        Assert.All(reporter.Tables.Single(), assertion => Assert.True(assertion.Passed));
        Assert.Contains("3 validations passed.", reporter.Passes);
    }

    [Fact(DisplayName = "Aggregate response validation keeps all failure rows in one table")]
    public void ResponseContractKeepsFailureRowsTogether()
    {
        var reporter = new RecordingReporter();

        Assert.Throws<ApiValidationException>(() => ApiAssertions.Contract(
            reporter,
            Response(HttpStatusCode.NotFound, "{\"message\":\"missing\"}"),
            "Validate: aggregated failure",
            contract => contract
                .Status(HttpStatusCode.OK)
                .JsonPathExists("$.id")
                .BodyContains("missing")));

        Assert.Single(reporter.Tables);
        Assert.Equal(3, reporter.Tables.Single().Count);
        Assert.Equal(2, reporter.Tables.Single().Count(assertion => !assertion.Passed));
    }

    [Fact(DisplayName = "Header and body assertions report useful actual values")]
    public void HeaderAndBodyAssertionsReportActualValues()
    {
        var reporter = new RecordingReporter();
        var response = Response(
            HttpStatusCode.OK,
            "{\"message\":\"created\"}",
            new Dictionary<string, string> { ["X-Correlation-Id"] = "corr-123", ["Content-Type"] = "application/json" });

        ApiAssertions.HeaderPresent(reporter, response, "X-Correlation-Id");
        ApiAssertions.BodyContains(reporter, response, "created");
        ApiAssertions.ContentTypeContains(reporter, response, "application/json");

        Assert.Equal(3, reporter.Tables.Count);
        Assert.All(reporter.Tables, rows => Assert.True(Assert.Single(rows).Passed));
        Assert.Contains("corr-123", reporter.Tables[0].Single().Actual, StringComparison.Ordinal);
        Assert.Contains("created", reporter.Tables[1].Single().Actual, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Assertion actual values remain complete before HTML rendering")]
    public void AssertionActualValuesRemainCompleteBeforeHtmlRendering()
    {
        var reporter = new RecordingReporter { AssertionMaxDisplayCharacters = 32 };
        var body = "[{\"user_id\":8663020,\"title\":\"" + new string('x', 200) + "\"}]";

        Assert.Throws<ApiValidationException>(() => JsonAssertions.EveryArrayItemEquals(
            reporter,
            Response(HttpStatusCode.OK, body),
            "$",
            "user_id",
            "different"));

        var actual = Assert.Single(reporter.Tables.Single()).Actual;
        Assert.Equal(256, actual.Length);
        Assert.Contains(new string('x', 200), actual, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "JSON path assertion fails with an explicit missing token")]
    public void JsonPathAssertionReportsMissingToken()
    {
        var reporter = new RecordingReporter();
        var response = Response(HttpStatusCode.OK, "{\"id\":1}");

        var exception = Assert.Throws<ApiValidationException>(() =>
            JsonAssertions.JsonPathExists(reporter, response, "$.missing"));

        Assert.Equal("JSONPath exists: $.missing", exception.Rule);
        Assert.Equal("<missing>", reporter.Tables.Single().Single().Actual);
        Assert.Contains("Validation failed.", reporter.Failures);
    }

    [Fact(DisplayName = "Every-array-item assertion detects a relationship mismatch")]
    public void RelationshipAssertionDetectsMismatch()
    {
        var reporter = new RecordingReporter();
        var response = Response(HttpStatusCode.OK, "[{\"user_id\":7},{\"user_id\":8}]");

        var exception = Assert.Throws<ApiValidationException>(() =>
            JsonAssertions.EveryArrayItemEquals(reporter, response, "$", "user_id", "7"));

        Assert.Equal("Every $ item user_id", exception.Rule);
        Assert.False(reporter.Tables.Single().Single().Passed);
    }

    [Fact(DisplayName = "Schema assertion validates a schema from an absolute path")]
    public void SchemaAssertionValidatesAbsoluteSchema()
    {
        var schemaFile = Path.Combine(Path.GetTempPath(), $"atf-schema-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(schemaFile, """
            {
              "type": "object",
              "required": ["id"],
              "properties": { "id": { "type": "integer" } }
            }
            """);
            var reporter = new RecordingReporter();

            JsonSchemaAssertions.Matches(reporter, Response(HttpStatusCode.OK, "{\"id\":42}"), schemaFile);

            var row = Assert.Single(reporter.Tables.Single());
            Assert.Equal("JSON schema", row.Name);
            Assert.True(row.Passed);
            Assert.Equal("schema matched", row.Actual);
        }
        finally
        {
            if (File.Exists(schemaFile)) File.Delete(schemaFile);
        }
    }

    [Fact(DisplayName = "Schema assertion keeps schema failures in the same assertion table")]
    public void SchemaAssertionKeepsFailureEvidence()
    {
        var schemaFile = Path.Combine(Path.GetTempPath(), $"atf-invalid-schema-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(schemaFile, "{\"type\":\"object\",\"required\":[\"id\"]}");
            var reporter = new RecordingReporter();

            Assert.Throws<ApiValidationException>(() => JsonSchemaAssertions.Matches(
                reporter,
                Response(HttpStatusCode.OK, "{\"name\":\"missing-id\"}"),
                schemaFile));

            var row = Assert.Single(reporter.Tables.Single());
            Assert.Equal("JSON schema", row.Name);
            Assert.False(row.Passed);
            Assert.Contains("Required properties are missing", row.Actual, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(schemaFile)) File.Delete(schemaFile);
        }
    }

    [Fact(DisplayName = "JSON response data extracts the first collection identifier")]
    public void JsonResponseDataExtractsFirstId()
    {
        var response = Response(HttpStatusCode.OK, "[{\"id\":123},{\"id\":456}]");

        Assert.Equal(123, JsonResponseData.FirstArrayItemInt(response, "id"));
    }

    [Theory(DisplayName = "JSON response data rejects unusable collection payloads")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("[{\"name\":\"no-id\"}]")]
    [InlineData("[{\"id\":\"not-an-int\"}]")]
    public void JsonResponseDataRejectsInvalidPayload(string body)
    {
        var exception = Assert.Throws<ApiValidationException>(() =>
            JsonResponseData.FirstArrayItemInt(Response(HttpStatusCode.OK, body), "id"));

        Assert.Contains("JSON collection", exception.Rule, StringComparison.Ordinal);
    }

    private static ApiResponse Response(
        HttpStatusCode status,
        string body,
        IReadOnlyDictionary<string, string>? headers = null) =>
        new(status, body, headers ?? new Dictionary<string, string>(), TimeSpan.Zero, "https://example.test");

    private sealed class RecordingReporter : IAtfReporter
    {
        public int AssertionMaxDisplayCharacters { get; init; } = ReportingDefaults.AssertionMaxDisplayCharacters;
        public List<IReadOnlyCollection<AtfAssertion>> Tables { get; } = [];
        public List<string> Failures { get; } = [];
        public List<string> Passes { get; } = [];

        public void StartStep(string title) { }
        public void CompleteStep(AtfReportStatus status) { }
        public void Detail(string key, string value) { }
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) => Tables.Add(assertions);
        public void Info(string message) { }
        public void Pass(string message) => Passes.Add(message);
        public void Fail(string message, Exception? exception = null) => Failures.Add(message);
        public void Warning(string message) { }
        public void Skip(string reason) { }
    }
}
