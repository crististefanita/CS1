using Atf.Api.Infrastructure;
using Atf.Api.TestHost;
using Atf.Common.Errors;
using Atf.Common.Validation;
using System.Net;
using System.Globalization;
using Xunit;
using Xunit.Sdk;

namespace Atf.Api;

[Collection("API ATF")]
public sealed class ApiLifecycleAndCombinationsTests(
    ITestOutputHelper output,
    ApiTestFixture fixture)
    : ApiTestBase(output, fixture)
{
    [Fact(DisplayName = "API lifecycle: warning followed by passing body remains warning")]
    [Trait("Category", "lifecycle-combination")]
    public Task WarningThenPassRemainsVisible()
    {
        return RunScenarioAsync(
            "Users API — warning → pass precedence",
            ["api", "users", "lifecycle", "warning"],
            setup: context =>
            {
                context.Reporter.Warning("Intentional warning before the successful body validation.");
                return Task.CompletedTask;
            },
            body: async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
            });
    }

    [Fact(DisplayName = "API lifecycle: warning followed by failed validation is authoritative")]
    [Trait("Category", "lifecycle-combination")]
    public async Task WarningThenFailureIsAuthoritative()
    {
        var exception = await Record.ExceptionAsync(() => RunScenarioAsync(
            "Users API — warning → fail precedence",
            ["api", "users", "lifecycle", "warning", "failure"],
            setup: context =>
            {
                context.Reporter.Warning("Intentional warning before the hard contract failure.");
                return Task.CompletedTask;
            },
            body: async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ApiAssertions.Status(context.Reporter, users.Response, HttpStatusCode.Created);
            },
            teardown: async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
            }));

        Assert.IsType<ApiValidationException>(exception);
    }

    [Fact(DisplayName = "API lifecycle: passing body followed by teardown failure remains visible")]
    [Trait("Category", "lifecycle-combination")]
    public async Task PassingBodyThenTeardownFailureIsVisible()
    {
        var exception = await Record.ExceptionAsync(() => RunScenarioAsync(
            "Users API — pass → teardown fail perspective",
            ["api", "users", "lifecycle", "failure", "teardown"],
            setup: null,
            body: async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
            },
            teardown: _ => throw new InvalidOperationException(
                "Intentional teardown failure after a passing request and validation.")));

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact(DisplayName = "API lifecycle: passing setup followed by warning body is warning")]
    [Trait("Category", "lifecycle-combination")]
    public Task PassThenWarningIsVisible()
    {
        return RunScenarioAsync(
            "Users API — pass → warning precedence",
            ["api", "users", "lifecycle", "warning"],
            setup: async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
            },
            body: async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
                context.Reporter.Warning("Intentional warning after a successful setup and body exchange.");
            });
    }

    [Fact(DisplayName = "API lifecycle: body-only scenario has no synthetic phases")]
    [Trait("Category", "lifecycle-combination")]
    public Task BodyOnlyLifecycleRemainsExplicit()
    {
        return RunScenarioAsync(
            "Users API — test only",
            ["api", "users", "lifecycle", "combination"],
            async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
            });
    }

    [Fact(DisplayName = "API lifecycle: setup skip still executes passing teardown")]
    [Trait("Category", "lifecycle-combination")]
    public async Task SetupSkipStillRunsPassingTeardown()
    {
        var exception = await Record.ExceptionAsync(() => RunScenarioAsync(
            "Users API — setup skip → teardown pass",
            ["api", "users", "lifecycle", "skip"],
            setup: _ => throw new ApiScenarioSkipException("Intentional setup skip for lifecycle precedence."),
            body: _ => Task.CompletedTask,
            teardown: async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
            }));

        Assert.IsType<SkipException>(exception);
    }

    [Fact(DisplayName = "API lifecycle: body skip still executes passing teardown")]
    [Trait("Category", "lifecycle-combination")]
    public async Task BodySkipStillRunsPassingTeardown()
    {
        var exception = await Record.ExceptionAsync(() => RunScenarioAsync(
            "Users API — body skip → teardown pass",
            ["api", "users", "lifecycle", "skip"],
            setup: null,
            body: _ => throw new ApiScenarioSkipException("Intentional body skip for lifecycle precedence."),
            teardown: async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
            }));

        Assert.IsType<SkipException>(exception);
    }

    [Fact(DisplayName = "API lifecycle: passing body followed by teardown warning is warning")]
    [Trait("Category", "lifecycle-combination")]
    public Task TeardownWarningPromotesScenario()
    {
        return RunScenarioAsync(
            "Users API — pass → teardown warning precedence",
            ["api", "users", "lifecycle", "warning"],
            setup: null,
            body: async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
            },
            teardown: context =>
            {
                context.Reporter.Warning("Intentional teardown warning after a successful body.");
                return Task.CompletedTask;
            });
    }

    [Fact(DisplayName = "API lifecycle: passing body followed by teardown skip is skipped")]
    [Trait("Category", "lifecycle-combination")]
    public async Task TeardownSkipRemainsAuthoritative()
    {
        var exception = await Record.ExceptionAsync(() => RunScenarioAsync(
            "Users API — pass → teardown skip precedence",
            ["api", "users", "lifecycle", "skip"],
            setup: null,
            body: async context =>
            {
                var users = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, users.Response);
            },
            teardown: _ => throw new ApiScenarioSkipException("Intentional teardown skip for lifecycle precedence.")));

        Assert.IsType<SkipException>(exception);
    }

    [Fact(DisplayName = "API lifecycle: setup, body and teardown are grouped")]
    [Trait("Category", "lifecycle")]
    public Task SetupBodyAndTeardownAreGrouped() 
    {
        var state = new LifecycleState();

        return RunScenarioAsync(
            "Users API — setup → body → teardown",
            ["api", "users", "lifecycle"],
            setup: async context =>
            {
                var list = await context.Users.ListAsync(authProfile: "anonymous");
                ContractValidators.UserCollection(context.Reporter, list.Response);
                state.UserId = JsonResponseData.FirstArrayItemInt(list.Response, "id");
                context.Reporter.Detail("fixture.userId", state.UserId.ToString(CultureInfo.InvariantCulture));
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
                ApiAssertions.Contract(
                    context.Reporter,
                    result.Response,
                    "Validate: teardown user status",
                    new ResponseContract().ExpectStatus(HttpStatusCode.OK));
            });
    }

    [Theory(DisplayName = "API lifecycle combination: {0} / setup={1} / teardown={2}")]
    [InlineData("users", true, true)]
    [InlineData("posts", true, true)]
    [InlineData("users", true, false)]
    [InlineData("posts", false, true)]
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

                ApiAssertions.Contract(
                    context.Reporter,
                    result.Response,
                    "Validate: teardown resource status",
                    new ResponseContract().ExpectStatus(HttpStatusCode.OK));
            }
            : null;

        return RunScenarioAsync(
            $"{resource.ToUpperInvariant()} API — {PhaseShape(includeSetup, includeTeardown)}",
            ["api", resource, "lifecycle", "combination"],
            setup,
            async context =>
            {
                if (state.Id == 0)
                {
                    var seed = resource.Equals("users", StringComparison.OrdinalIgnoreCase)
                        ? await context.Users.ListAsync(authProfile: "anonymous")
                        : await context.Posts.ListAsync(authProfile: "anonymous");
                    if (resource.Equals("users", StringComparison.OrdinalIgnoreCase))
                    {
                        ContractValidators.UserCollection(context.Reporter, seed.Response);
                    }
                    else
                    {
                        ContractValidators.PostCollection(context.Reporter, seed.Response);
                    }
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
                    ApiAssertions.Contract(
                        context.Reporter,
                        post.Response,
                        "Validate: post item response",
                        contract => contract
                            .Status(HttpStatusCode.OK)
                            .JsonPathExists("$.id")
                            .JsonPathExists("$.user_id"));
                }
            },
            teardown);

        static string PhaseShape(bool setup, bool teardown) => (setup, teardown) switch
        {
            (true, true) => "setup → test → teardown",
            (true, false) => "setup → test",
            (false, true) => "test → teardown",
            _ => throw new InvalidOperationException("A lifecycle combination must include setup or teardown.")
        };
    }

    [Fact(DisplayName = "API lifecycle: setup, test and cleanup teardown preserve the resource contract")]
    [Trait("Category", "lifecycle")]
    public Task UserLifecycleUsesCleanupTeardown()
    {
        var state = new WriteLifecycleState();

        return RunScenarioAsync(
            "Users lifecycle — setup → test → cleanup teardown",
            ["api", "users", "lifecycle", "write"],
            setup: async context =>
            {
                RequireWriteToken(context);
                state.Request = Atf.Common.Data.UserDataFactory.Valid("lifecycle");
                var created = await context.Users.CreateAsync(state.Request);
                ApiAssertions.Contract(
                    context.Reporter,
                    created.Response,
                    "Validate: created user response",
                    contract => contract
                        .Status(HttpStatusCode.Created)
                        .JsonSchema("Schemas/user.schema.json"));

                state.UserId = System.Text.Json.JsonDocument.Parse(created.Response.Body)
                    .RootElement.GetProperty("id")
                    .GetInt32();

                context.Cleanup.Own("users", state.UserId.ToString(CultureInfo.InvariantCulture), async () =>
                {
                    var cleanup = await context.Users.DeleteAsync(state.UserId);
                    if (cleanup.Response.Status is not ((int)HttpStatusCode.NoContent) and not ((int)HttpStatusCode.NotFound))
                    {
                        throw new InvalidOperationException($"Cleanup delete returned {cleanup.Response.Status}.");
                    }
                });

                context.Reporter.Detail("fixture.userId", state.UserId.ToString(CultureInfo.InvariantCulture));
                context.Reporter.Detail("fixture.cleanup", "registered");
            },
            body: async context =>
            {
                var get = await context.Users.GetAsync(state.UserId);
                ContractValidators.UserItem(context.Reporter, get.Response);

                var patched = await context.Users.PatchAsync(
                    state.UserId,
                    state.Request with { Status = "inactive" });
                ApiAssertions.Contract(
                    context.Reporter,
                    patched.Response,
                    "Validate: patched user response",
                    new ResponseContract().ExpectStatus(HttpStatusCode.OK));

                var deleted = await context.Users.DeleteAsync(state.UserId);
                ApiAssertions.Contract(
                    context.Reporter,
                    deleted.Response,
                    "Validate: deleted user response",
                    new ResponseContract().ExpectStatus(HttpStatusCode.NoContent));

                var afterDelete = await context.Users.GetAsync(state.UserId);
                ApiAssertions.Contract(
                    context.Reporter,
                    afterDelete.Response,
                    "Validate: deleted user not found",
                    new ResponseContract().ExpectStatus(HttpStatusCode.NotFound));
            });
    }

    private sealed class LifecycleState
    {
        public int UserId { get; set; }
    }

    private sealed class WriteLifecycleState
    {
        public int UserId { get; set; }
        public Atf.Common.Models.UserRequest Request { get; set; } = null!;
    }

    private sealed class CombinationState(string resource)
    {
        public string Resource { get; } = resource;
        public int Id { get; set; }
    }
}
