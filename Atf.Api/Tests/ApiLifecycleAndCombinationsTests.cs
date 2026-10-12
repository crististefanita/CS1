using Atf.Api.Infrastructure;
using Atf.Common.Validation;
using System.Net;
using Xunit;

namespace Atf.Api;

[Collection("API ATF")]
public sealed class ApiLifecycleAndCombinationsTests(
    ITestOutputHelper output,
    Atf.Core.Reporting.ExtentCollectionFixture fixture)
    : ApiTestBase(output, fixture)
{
    [Fact(DisplayName = "API lifecycle: setup, body and teardown are grouped")]
    [Trait("Category", "lifecycle")]
    public Task SetupBodyAndTeardownAreGrouped() 
    {
        var state = new LifecycleState();

        return RunScenarioAsync(
            "Users API — setup → body → teardown",
            ["api", "users", "lifecycle", "framework"],
            setup: async context =>
            {
                var list = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, list.Response);
                state.UserId = JsonResponseData.FirstArrayItemInt(list.Response, "id");
                context.Reporter.Detail("fixture.userId", state.UserId.ToString());
                context.Reporter.Detail("fixture.source", "GET /public/v2/users");
            },
            body: async context =>
            {
                var result = await context.Users.GetAsync(state.UserId, "anonymous");
                ContractValidators.UserItem(context.Reporter, result.Response);
            },
            teardown: async context =>
            {
                var result = await context.Users.GetAsync(state.UserId, "anonymous");
                ApiAssertions.Status(context.Reporter, result.Response, HttpStatusCode.OK);
            });
    }

    [Theory(DisplayName = "API combination: collection contract for {0}")]
    [InlineData("users")]
    [InlineData("posts")]
    [Trait("Category", "combination")]
    public Task CollectionContractSupportsConfiguredResource(string resource) => RunScenarioAsync(
        $"GET /public/v2/{resource} — combination resource={resource}",
        ["api", "smoke", "combination", "contract"],
        async context =>
        {
            if (resource.Equals("users", StringComparison.OrdinalIgnoreCase))
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
                return;
            }

            var posts = await context.Posts.ListAsync(authProfile: "anonymous");
            ContractValidators.PostCollection(context.Reporter, posts.Response);
        });

    [Theory(DisplayName = "API lifecycle combination: {0} / setup={1} / teardown={2}")]
    [InlineData("users", true, true)]
    [InlineData("posts", true, true)]
    [InlineData("users", true, false)]
    [InlineData("posts", false, true)]
    [InlineData("users", false, false)]
    [Trait("Category", "lifecycle-combination")]
    public Task LifecyclePhaseCombinationsRemainNested(
        string resource,
        bool includeSetup,
        bool includeTeardown)
    {
        var state = new CombinationState(resource);

        Func<ApiScenarioContext, Task>? setup = includeSetup
            ? async context =>
            {
                if (resource.Equals("users", StringComparison.OrdinalIgnoreCase))
                {
                    var users = await context.Users.ListAsync(authProfile: "anonymous");
                    ContractValidators.UserCollection(context.Reporter, users.Response);
                    state.Id = JsonResponseData.FirstArrayItemInt(users.Response, "id");
                }
                else
                {
                    var posts = await context.Posts.ListAsync(authProfile: "anonymous");
                    ContractValidators.PostCollection(context.Reporter, posts.Response);
                    state.Id = JsonResponseData.FirstArrayItemInt(posts.Response, "id");
                }

                context.Reporter.Detail("combination.resource", resource);
                context.Reporter.Detail("combination.phaseShape", PhaseShape(includeSetup, includeTeardown));
            }
            : null;

        Func<ApiScenarioContext, Task>? teardown = includeTeardown
            ? async context =>
            {
                var result = resource.Equals("users", StringComparison.OrdinalIgnoreCase)
                    ? await context.Users.GetAsync(state.Id, "anonymous")
                    : await context.Posts.GetAsync(state.Id, "anonymous");

                ApiAssertions.Status(context.Reporter, result.Response, HttpStatusCode.OK);
            }
            : null;

        return RunScenarioAsync(
            $"{resource.ToUpperInvariant()} API — {PhaseShape(includeSetup, includeTeardown)}",
            ["api", resource, "lifecycle", "combination", "framework"],
            setup,
            async context =>
            {
                if (state.Id == 0)
                {
                    var seed = resource.Equals("users", StringComparison.OrdinalIgnoreCase)
                        ? await context.Users.ListAsync(authProfile: "anonymous")
                        : await context.Posts.ListAsync(authProfile: "anonymous");
                    state.Id = JsonResponseData.FirstArrayItemInt(seed.Response, "id");
                }

                if (resource.Equals("users", StringComparison.OrdinalIgnoreCase))
                {
                    var user = await context.Users.GetAsync(state.Id, "anonymous");
                    ContractValidators.UserItem(context.Reporter, user.Response);
                }
                else
                {
                    var post = await context.Posts.GetAsync(state.Id, "anonymous");
                    ApiAssertions.Status(context.Reporter, post.Response, HttpStatusCode.OK);
                    JsonAssertions.JsonPathExists(context.Reporter, post.Response, "$.id");
                    JsonAssertions.JsonPathExists(context.Reporter, post.Response, "$.user_id");
                }
            },
            teardown);

        static string PhaseShape(bool setup, bool teardown) => (setup, teardown) switch
        {
            (true, true) => "setup → test → teardown",
            (true, false) => "setup → test",
            (false, true) => "test → teardown",
            _ => "test only"
        };
    }

    private sealed class LifecycleState
    {
        public int UserId { get; set; }
    }

    private sealed class CombinationState(string resource)
    {
        public string Resource { get; } = resource;
        public int Id { get; set; }
    }
}
