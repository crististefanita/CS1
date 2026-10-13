using Atf.Core.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Atf.Common.Validation;

/// <summary>
/// Reusable contract checks. They only add rows to a ValidationTable; the
/// table decides when the single Extent validation step is reported.
/// </summary>
internal static class ValidationChecks
{
    public static void Status(ValidationTable table, ApiResponse response, HttpStatusCode expected)
    {
        table.Check(
            "HTTP status",
            $"{(int)expected} {expected}",
            $"{response.Status} {response.StatusCode}",
            () => response.StatusCode == expected);
    }

    public static void StatusOneOf(ValidationTable table, ApiResponse response, IReadOnlyCollection<HttpStatusCode> expected)
    {
        var expectedText = string.Join(" or ", expected.Select(status => $"{(int)status} {status}"));
        table.Check(
            "HTTP status",
            expectedText,
            $"{response.Status} {response.StatusCode}",
            () => expected.Contains(response.StatusCode));
    }

    public static void ContentTypeContains(ValidationTable table, ApiResponse response, string expected)
    {
        var actual = response.Header("Content-Type") ?? "<missing>";
        table.Check(
            "Content-Type",
            $"contains {expected}",
            actual,
            () => actual.Contains(expected, StringComparison.OrdinalIgnoreCase));
    }

    public static void HeaderPresent(ValidationTable table, ApiResponse response, string name)
    {
        var actual = response.Header(name) ?? "<missing>";
        table.Check(
            $"Header {name}",
            "present and non-blank",
            actual,
            () => !string.IsNullOrWhiteSpace(response.Header(name)));
    }

    public static void BodyContains(ValidationTable table, ApiResponse response, string expected)
    {
        table.Check(
            "Response body",
            $"contains {expected}",
            response.Body,
            () => response.Body.Contains(expected, StringComparison.OrdinalIgnoreCase));
    }

    public static void JsonPathExists(ValidationTable table, ApiResponse response, string path)
    {
        TryJson(table, response, $"JSONPath exists: {path}", "token exists", path, token => token is not null);
    }

    public static void JsonPathEquals(ValidationTable table, ApiResponse response, string path, string expected)
    {
        TryJson(table, response, $"JSONPath equals: {path}", expected, path,
            token => string.Equals(token?.ToString(), expected, StringComparison.Ordinal));
    }

    public static void EveryArrayItemEquals(
        ValidationTable table,
        ApiResponse response,
        string path,
        string property,
        string expected)
    {
        TryJson(table, response, $"Every {path} item {property}", expected, path,
            token => token is JArray array &&
                     array.All(item => string.Equals(item[property]?.ToString(), expected, StringComparison.Ordinal)));
    }

    public static void JsonSchema(ValidationTable table, ApiResponse response, string schemaPath)
    {
        try
        {
            var schemaFile = Path.IsPathRooted(schemaPath)
                ? schemaPath
                : Path.Combine(AppContext.BaseDirectory, schemaPath);
            var schema = JSchema.Parse(File.ReadAllText(schemaFile));
            var json = JToken.Parse(response.Body);
            var valid = json.IsValid(schema, out IList<string> messages);
            var actual = valid ? "schema matched" : string.Join(" | ", messages);
            table.Add("JSON schema", schemaPath, actual, valid);
        }
        catch (Exception exception)
        {
            table.AddFailure("JSON schema", schemaPath, response.Body, exception);
        }
    }

    private static void TryJson(
        ValidationTable table,
        ApiResponse response,
        string name,
        string expected,
        string path,
        Func<JToken?, bool> predicate)
    {
        try
        {
            var root = JToken.Parse(response.Body);
            var token = root.SelectToken(path);
            var actual = token?.ToString() ?? "<missing>";
            table.Add(name, expected, actual, predicate(token));
        }
        catch (Exception exception)
        {
            table.AddFailure(name, expected, response.Body, exception);
        }
    }
}
