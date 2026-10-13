using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;

namespace Atf.Common.Validation;

public static class ApiAssertions
{
    /// <summary>
    /// Publishes one aggregate evidence table for all assertions about one response.
    /// Do not pass assertions for another HTTP response to the same group.
    /// </summary>
    public static void Contract(
        IAtfValidationReporter reporter,
        ApiResponse response,
        string title,
        Action<ResponseContract> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var contract = new ResponseContract();
        configure(contract);
        Contract(reporter, response, title, contract);
    }

    /// <summary>
    /// Publishes one aggregate validation table for one response contract.
    /// This overload is the preferred composition boundary for reusable
    /// contract definitions.
    /// </summary>
    public static void Contract(
        IAtfValidationReporter reporter,
        ApiResponse response,
        string title,
        ResponseContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        ValidationTable.Run(reporter, title, table =>
            contract.Evaluate(response, table));
    }

    /// <summary>
    /// Evaluates one response contract without throwing when assertion rows
    /// fail. The aggregate table is still published with failed rows and the
    /// reporter receives a Warning, making the scenario visible as Other in
    /// the ATF Overview. Unexpected validator/configuration exceptions still
    /// propagate as real test failures.
    /// </summary>
    public static void SoftContract(
        IAtfValidationReporter reporter,
        ApiResponse response,
        string title,
        Action<ResponseContract> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var contract = new ResponseContract();
        configure(contract);
        SoftContract(reporter, response, title, contract);
    }

    /// <summary>
    /// Warning-severity counterpart of <see cref="Contract(IAtfValidationReporter, ApiResponse, string, ResponseContract)"/>.
    /// </summary>
    public static void SoftContract(
        IAtfValidationReporter reporter,
        ApiResponse response,
        string title,
        ResponseContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        ValidationTable.RunWarning(reporter, title, table =>
            contract.Evaluate(response, table));
    }

    public static void Status(IAtfValidationReporter reporter, ApiResponse response, HttpStatusCode expected)
    {
        Contract(reporter, response, $"Validate: status={(int)expected}",
            contract => contract.Status(expected));
    }

    public static void StatusOneOf(IAtfValidationReporter reporter, ApiResponse response, params HttpStatusCode[] expected)
    {
        Contract(reporter, response, "Validate: status is allowed",
            contract => contract.StatusOneOf(expected));
    }

    public static void ContentTypeContains(IAtfValidationReporter reporter, ApiResponse response, string expected)
    {
        Contract(reporter, response, $"Validate: content-type contains {expected}",
            contract => contract.ContentTypeContains(expected));
    }

    public static void HeaderPresent(IAtfValidationReporter reporter, ApiResponse response, string name)
    {
        Contract(reporter, response, $"Validate: header present={name}",
            contract => contract.HeaderPresent(name));
    }

    public static void BodyContains(IAtfValidationReporter reporter, ApiResponse response, string expected)
    {
        Contract(reporter, response, $"Validate: body contains {expected}",
            contract => contract.BodyContains(expected));
    }
}
