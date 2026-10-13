using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;

namespace Atf.Common.Validation;

public static class ContractValidators
{
    public static void UserItem(IAtfValidationReporter reporter, ApiResponse response)
    {
        ApiAssertions.Contract(reporter, response, "Validate: user item contract", UserItemContract());
    }

    public static void UserCollection(IAtfValidationReporter reporter, ApiResponse response)
    {
        ApiAssertions.Contract(reporter, response, "Validate: user collection contract", UserCollectionContract());
    }

    public static void PostItem(IAtfValidationReporter reporter, ApiResponse response)
    {
        ApiAssertions.Contract(reporter, response, "Validate: post item contract", PostItemContract());
    }

    public static void PostCollection(IAtfValidationReporter reporter, ApiResponse response)
    {
        ApiAssertions.Contract(reporter, response, "Validate: post collection contract", PostCollectionContract());
    }

    public static void UserPostsBelongToUser(IAtfValidationReporter reporter, ApiResponse response, int userId)
    {
        ApiAssertions.Contract(reporter, response, "Validate: user posts relationship contract", UserPostsBelongToUserContract(userId));
    }

    public static ResponseContract UserItemContract() => new ResponseContract()
        .ExpectStatus(HttpStatusCode.OK)
        .ExpectContentType("application/json")
        .ExpectSchema("Schemas/user.schema.json")
        .ExpectJsonPath("$.id");

    public static ResponseContract UserCollectionContract() => new ResponseContract()
        .ExpectStatus(HttpStatusCode.OK)
        .ExpectContentType("application/json")
        .ExpectSchema("Schemas/user-collection.schema.json");

    public static ResponseContract PostItemContract() => new ResponseContract()
        .ExpectStatus(HttpStatusCode.OK)
        .ExpectContentType("application/json")
        .ExpectJsonPath("$.id")
        .ExpectJsonPath("$.user_id");

    public static ResponseContract PostCollectionContract() => new ResponseContract()
        .ExpectStatus(HttpStatusCode.OK)
        .ExpectContentType("application/json")
        .ExpectSchema("Schemas/post-collection.schema.json");

    public static ResponseContract UserPostsBelongToUserContract(int userId) => new ResponseContract()
        .ExpectStatus(HttpStatusCode.OK)
        .ExpectContentType("application/json")
        .ExpectEveryArrayItem("$", "user_id", userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
