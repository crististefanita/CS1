using Atf.Core.Http;
using Atf.Core.Reporting;
using Atf.Common.Validation.Groups;
using System.Net;

namespace Atf.Common.Validation;

public static class ApiAssertions
{
    /// <summary>
    /// Publishes one aggregate evidence table for all assertions about one response.
    /// Do not pass assertions for another HTTP response to the same group.
    /// </summary>
    public static void Contract(
        IAtfReporter reporter,
        ApiResponse response,
        string title,
        Action<ResponseValidationGroup> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var contract = new ResponseContract();
        configure(new ResponseValidationGroup(contract));
        Contract(reporter, response, title, contract);
    }

    /// <summary>
    /// Publishes one aggregate validation table for one response contract.
    /// This overload is the preferred composition boundary for reusable
    /// contract definitions.
    /// </summary>
    public static void Contract(
        IAtfReporter reporter,
        ApiResponse response,
        string title,
        ResponseContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        ValidationTable.Run(reporter, title, table =>
            contract.Evaluate(response, table));
    }

    public static void Status(IAtfReporter reporter, ApiResponse response, HttpStatusCode expected)
    {
        Contract(reporter, response, $"Validate: status={(int)expected}",
            group => group.Status(expected));
    }

    public static void StatusOneOf(IAtfReporter reporter, ApiResponse response, params HttpStatusCode[] expected)
    {
        Contract(reporter, response, "Validate: status is allowed",
            group => group.StatusOneOf(expected));
    }

    public static void ContentTypeContains(IAtfReporter reporter, ApiResponse response, string expected)
    {
        Contract(reporter, response, $"Validate: content-type contains {expected}",
            group => group.ContentTypeContains(expected));
    }

    public static void HeaderPresent(IAtfReporter reporter, ApiResponse response, string name)
    {
        Contract(reporter, response, $"Validate: header present={name}",
            group => group.HeaderPresent(name));
    }

    public static void BodyContains(IAtfReporter reporter, ApiResponse response, string expected)
    {
        Contract(reporter, response, $"Validate: body contains {expected}",
            group => group.BodyContains(expected));
    }
}
