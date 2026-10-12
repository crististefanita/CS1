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
            ApiAssertions.StatusOneOf(context.Reporter, result.Response, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        });

    [Fact(DisplayName = "Users: lifecycle create, get, patch, delete and verify deletion")]
    public Task UserLifecycleIsConsistent() => RunScenarioAsync(
        "Users lifecycle — create → get → patch → delete → verify",
        ["api", "users", "lifecycle", "write"],
        async context =>
        {
            RequireWriteToken(context);
            var request = Atf.Common.Data.UserDataFactory.Valid("lifecycle");
            var created = await context.Users.CreateAsync(request);
            ApiAssertions.Status(context.Reporter, created.Response, HttpStatusCode.Created);
            JsonSchemaAssertions.Matches(context.Reporter, created.Response, "Schemas/user.schema.json");
            var id = System.Text.Json.JsonDocument.Parse(created.Response.Body).RootElement.GetProperty("id").GetInt32();
            context.Cleanup.Own("users", id.ToString(), async () =>
            {
                var cleanup = await context.Users.DeleteAsync(id);
                if (cleanup.Response.Status is not ((int)HttpStatusCode.NoContent) and not ((int)HttpStatusCode.NotFound))
                {
                    throw new InvalidOperationException($"Cleanup delete returned {cleanup.Response.Status}.");
                }
            });

            var get = await context.Users.GetAsync(id);
            ContractValidators.UserItem(context.Reporter, get.Response);

            var patched = await context.Users.PatchAsync(id, request with { Status = "inactive" });
            ApiAssertions.Status(context.Reporter, patched.Response, HttpStatusCode.OK);

            var deleted = await context.Users.DeleteAsync(id);
            ApiAssertions.Status(context.Reporter, deleted.Response, HttpStatusCode.NoContent);
            var afterDelete = await context.Users.GetAsync(id);
            ApiAssertions.Status(context.Reporter, afterDelete.Response, HttpStatusCode.NotFound);
        });
}
