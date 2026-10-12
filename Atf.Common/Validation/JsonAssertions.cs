using Atf.Core.Http;
using Atf.Core.Reporting;

namespace Atf.Common.Validation;

public static class JsonAssertions
{
    public static void JsonPathExists(IAtfReporter reporter, ApiResponse response, string path)
    {
        ApiAssertions.Contract(reporter, response, $"Validate: JSONPath exists={path}",
            group => group.JsonPathExists(path));
    }

    public static void JsonPathEquals(IAtfReporter reporter, ApiResponse response, string path, string expected)
    {
        ApiAssertions.Contract(reporter, response, $"Validate: JSONPath {path}={expected}",
            group => group.JsonPathEquals(path, expected));
    }

    public static void EveryArrayItemEquals(
        IAtfReporter reporter,
        ApiResponse response,
        string path,
        string property,
        string expected)
    {
        ApiAssertions.Contract(reporter, response, $"Validate: every {path} item {property}={expected}",
            group => group.EveryArrayItemEquals(path, property, expected));
    }
}
