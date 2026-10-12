using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;

namespace Atf.Common.Validation;

public static class ApiAssertions
{
    public static void Status(IAtfReporter reporter, ApiResponse response, HttpStatusCode expected)
    {
        ValidationTable.Run(reporter, $"Validate: status={(int)expected}", table =>
            ValidationChecks.Status(table, response, expected));
    }

    public static void StatusOneOf(IAtfReporter reporter, ApiResponse response, params HttpStatusCode[] expected)
    {
        ValidationTable.Run(reporter, "Validate: status is allowed", table =>
            ValidationChecks.StatusOneOf(table, response, expected));
    }

    public static void ContentTypeContains(IAtfReporter reporter, ApiResponse response, string expected)
    {
        ValidationTable.Run(reporter, $"Validate: content-type contains {expected}", table =>
            ValidationChecks.ContentTypeContains(table, response, expected));
    }

    public static void HeaderPresent(IAtfReporter reporter, ApiResponse response, string name)
    {
        ValidationTable.Run(reporter, $"Validate: header present={name}", table =>
            ValidationChecks.HeaderPresent(table, response, name));
    }

    public static void BodyContains(IAtfReporter reporter, ApiResponse response, string expected)
    {
        ValidationTable.Run(reporter, $"Validate: body contains {expected}", table =>
            ValidationChecks.BodyContains(table, response, expected));
    }
}
