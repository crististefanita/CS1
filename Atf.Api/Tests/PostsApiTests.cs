using Atf.Common.Validation;
using Atf.Api.Infrastructure;
using System.Net;
using Xunit;

namespace Atf.Api;

[Collection("API ATF")]
public sealed class PostsApiTests(ITestOutputHelper output, Atf.Core.Reporting.ExtentCollectionFixture fixture)
    : ApiTestBase(output, fixture)
{
    [Fact(DisplayName = "Posts: list returns a valid collection contract")]
    [Trait("Category", "smoke")]
    public Task ListPostsReturnsCollection() => RunScenarioAsync(
        "GET /public/v2/posts — collection contract",
        ["api", "posts", "smoke", "contract"],
        async context =>
        {
            var result = await context.Posts.ListAsync(authProfile: "anonymous");
            ContractValidators.PostCollection(context.Reporter, result.Response);
        });

    [Fact(DisplayName = "Posts: known post can be retrieved")]
    [Trait("Category", "smoke")]
    public Task GetKnownPostReturnsItem() => RunScenarioAsync(
        "GET /public/v2/posts/{id} — item contract",
        ["api", "posts", "smoke", "contract"],
        async context =>
        {
            var list = await context.Posts.ListAsync(authProfile: "anonymous");
            ContractValidators.PostCollection(context.Reporter, list.Response);
            var id = JsonResponseData.FirstArrayItemInt(list.Response, "id");
            var result = await context.Posts.GetAsync(id, "anonymous");
            ApiAssertions.Status(context.Reporter, result.Response, HttpStatusCode.OK);
            JsonAssertions.JsonPathExists(context.Reporter, result.Response, "$.id");
            JsonAssertions.JsonPathExists(context.Reporter, result.Response, "$.user_id");
        });

    [Fact(DisplayName = "Posts: nested user posts preserve parent relationship")]
    public Task UserPostsPreserveParentRelationship() => RunScenarioAsync(
        "GET /public/v2/users/{userId}/posts — relationship contract",
        ["api", "posts", "relationship", "contract"],
        async context =>
        {
            var posts = await context.Posts.ListAsync(authProfile: "anonymous");
            ContractValidators.PostCollection(context.Reporter, posts.Response);
            var userId = JsonResponseData.FirstArrayItemInt(posts.Response, "user_id");
            var result = await context.Posts.ListForUserAsync(userId, authProfile: "anonymous");
            ContractValidators.UserPostsBelongToUser(context.Reporter, result.Response, userId);
        });

    [Fact(DisplayName = "Posts: unknown post returns not found")]
    public Task UnknownPostReturnsNotFound() => RunScenarioAsync(
        "GET /public/v2/posts/{unknownId} — not found contract",
        ["api", "posts", "negative", "contract"],
        async context =>
        {
            var result = await context.Posts.GetAsync(999999999, "anonymous");
            ApiAssertions.Status(context.Reporter, result.Response, HttpStatusCode.NotFound);
        });
}
