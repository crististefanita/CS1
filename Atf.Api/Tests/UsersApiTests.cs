using Atf.Common.Validation;
using Atf.Api.Infrastructure;
using System.Net;
using Xunit;

namespace Atf.Api;

[Collection("API ATF")]
public sealed class UsersApiTests(ITestOutputHelper output, Atf.Core.Reporting.ExtentCollectionFixture fixture)
    : ApiTestBase(output, fixture)
{
    [Fact(DisplayName = "Users: list returns a valid JSON collection")]
    [Trait("Category", "smoke")]
    public Task ListUsersReturnsCollection() => RunScenarioAsync(
        "GET /public/v2/users — collection contract",
        ["api", "users", "smoke", "contract"],
        async context =>
        {
            var result = await context.Users.ListAsync(authProfile: "anonymous");
            ContractValidators.UserCollection(context.Reporter, result.Response);
        });

    [Fact(DisplayName = "Users: known user can be retrieved and matches schema")]
    [Trait("Category", "smoke")]
    public Task GetKnownUserReturnsItem() => RunScenarioAsync(
        "GET /public/v2/users/{id} — item contract",
        ["api", "users", "smoke", "contract"],
        async context =>
        {
            var list = await context.Users.ListAsync(authProfile: "anonymous");
            ContractValidators.UserCollection(context.Reporter, list.Response);
            var id = JsonResponseData.FirstArrayItemInt(list.Response, "id");
            var result = await context.Users.GetAsync(id, "anonymous");
            ContractValidators.UserItem(context.Reporter, result.Response);
        });

    [Fact(DisplayName = "Users: missing authentication is rejected for write")]
    public Task MissingAuthenticationIsRejectedForWrite() => RunScenarioAsync(
        "POST /public/v2/users — missing authentication",
        ["api", "users", "negative", "auth"],
        async context =>
        {
            var request = Atf.Common.Data.UserDataFactory.WithEmail("not-authorized@example.com", "anonymous");
            var result = await context.Users.CreateAsync(request, "anonymous");
            ApiAssertions.Contract(
                context.Reporter,
                result.Response,
                "Validate: missing authentication",
                new ResponseContract()
                    .ExpectStatusOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden));
        });

}
