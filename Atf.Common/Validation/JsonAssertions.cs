using Atf.Core.Http;
using Atf.Core.Reporting;

namespace Atf.Common.Validation;

public static class JsonAssertions
{
    public static void JsonPathExists(IAtfReporter reporter, ApiResponse response, string path)
    {
        ValidationTable.Run(reporter, $"Validate: JSONPath exists={path}", table =>
            ValidationChecks.JsonPathExists(table, response, path));
    }

    public static void JsonPathEquals(IAtfReporter reporter, ApiResponse response, string path, string expected)
    {
        ValidationTable.Run(reporter, $"Validate: JSONPath {path}={expected}", table =>
            ValidationChecks.JsonPathEquals(table, response, path, expected));
    }

    public static void EveryArrayItemEquals(
        IAtfReporter reporter,
        ApiResponse response,
        string path,
        string property,
        string expected)
    {
        ValidationTable.Run(reporter, $"Validate: every {path} item {property}={expected}", table =>
            ValidationChecks.EveryArrayItemEquals(table, response, path, property, expected));
    }
}
