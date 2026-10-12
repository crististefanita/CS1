using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;

namespace Atf.Common.Validation;

public static class ContractValidators
{
    public static void UserItem(IAtfReporter reporter, ApiResponse response)
    {
        ValidationTable.Run(reporter, "Validate: user item contract", table =>
        {
            ValidationChecks.Status(table, response, HttpStatusCode.OK);
            ValidationChecks.ContentTypeContains(table, response, "application/json");
            ValidationChecks.JsonSchema(table, response, "Schemas/user.schema.json");
            ValidationChecks.JsonPathExists(table, response, "$.id");
        });
    }

    public static void UserCollection(IAtfReporter reporter, ApiResponse response)
    {
        ValidationTable.Run(reporter, "Validate: user collection contract", table =>
        {
            ValidationChecks.Status(table, response, HttpStatusCode.OK);
            ValidationChecks.ContentTypeContains(table, response, "application/json");
            ValidationChecks.JsonSchema(table, response, "Schemas/user-collection.schema.json");
        });
    }

    public static void PostCollection(IAtfReporter reporter, ApiResponse response)
    {
        ValidationTable.Run(reporter, "Validate: post collection contract", table =>
        {
            ValidationChecks.Status(table, response, HttpStatusCode.OK);
            ValidationChecks.ContentTypeContains(table, response, "application/json");
            ValidationChecks.JsonSchema(table, response, "Schemas/post-collection.schema.json");
        });
    }

    public static void UserPostsBelongToUser(IAtfReporter reporter, ApiResponse response, int userId)
    {
        ValidationTable.Run(reporter, "Validate: user posts relationship contract", table =>
        {
            ValidationChecks.Status(table, response, HttpStatusCode.OK);
            ValidationChecks.ContentTypeContains(table, response, "application/json");
            ValidationChecks.EveryArrayItemEquals(table, response, "$", "user_id", userId.ToString());
        });
    }
}
