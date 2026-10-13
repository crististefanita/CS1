using Atf.Common.Data;
using Atf.Common.Models;
using Atf.Common.Modules;
using Atf.Common.Routing;
using Atf.Common.Services;
using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;
using System.Text.Json;

namespace Atf.Common.Tests;

public sealed class RoutingServicesAndDataTests
{
    [Fact(DisplayName = "Route templates safely encode path tokens")]
    public void RouteTemplateEncodesPathToken()
    {
        var route = new ApiRoute("custom", HttpMethod.Get, "/items/{id}", "items", "get", ApiRouteAuthentication.Anonymous());

        Assert.Equal("/items/a%2Fb%20c", route.ResolvePath(("id", "a/b c")));
    }

    [Fact(DisplayName = "Route resolution rejects unresolved path tokens before transport")]
    public void RouteResolutionRejectsMissingPathToken()
    {
        var route = new ApiRoute("custom", HttpMethod.Get, "/items/{id}", "items", "get", ApiRouteAuthentication.Anonymous());

        var exception = Assert.Throws<InvalidOperationException>(() => route.ResolvePath());

        Assert.Contains("unresolved path tokens", exception.Message, StringComparison.Ordinal);
        Assert.Contains("{id}", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Route request preserves query, auth profile and semantic metadata")]
    public void RouteRequestPreservesMetadata()
    {
        var route = new ApiRoute("users.list", HttpMethod.Get, "/users", "users", "list", ApiRouteAuthentication.Anonymous());

        var request = route.CreateRequest(
            authProfile: "custom",
            query: new Dictionary<string, string?> { ["page"] = "2" });

        Assert.Equal("custom", request.AuthProfile);
        Assert.Equal("2", request.Query?["page"]);
        Assert.Same(route, request.Route);
    }

    [Fact(DisplayName = "Route authentication metadata is typed and report-safe")]
    public void RouteAuthenticationMetadataIsTyped()
    {
        var route = new ApiRoute(
            "users.create",
            HttpMethod.Post,
            "/users",
            "users",
            "create",
            ApiRouteAuthentication.Required(ApiAuthType.Bearer, "default"));

        Assert.Equal(ApiAuthType.Bearer, route.AuthType);
        Assert.Equal(ApiAuthRequirement.Required, route.AuthRequirement);
        Assert.Equal("default", route.DefaultAuthProfile);
        Assert.Equal("bearer", route.Authentication.TypeName);
        Assert.Equal("bearer required", route.AuthPolicy);
        Assert.False(route.Authentication.IsAnonymousAllowed);
    }

    [Fact(DisplayName = "Invalid route authentication combinations fail during composition")]
    public void InvalidRouteAuthenticationFailsFast()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new ApiRouteAuthentication(ApiAuthType.None, ApiAuthRequirement.Required, "default"));

        Assert.Contains("mechanism is required", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Users service serializes a web-shaped request and delegates to the create route")]
    public async Task UsersServiceSerializesCreateRequest()
    {
        var executor = new RecordingExecutor();
        var routes = new ApiRoutes(new RouteSettings());
        var service = new UsersService(executor, routes);
        var request = new UserRequest("Ada", "ada@example.com", "female", "active");

        await service.CreateAsync(request, "write", TestContext.Current.CancellationToken);

        Assert.Equal(ApiRequestConstants.Post, executor.Request!.Method);
        Assert.Equal(RouteSettings.DefaultUsersPath, executor.Request.Path);
        Assert.Equal("write", executor.Request.AuthProfile);
        Assert.Equal("Ada", JsonDocument.Parse(executor.Request.BodyJson!).RootElement.GetProperty("name").GetString());
        Assert.Equal("ada@example.com", JsonDocument.Parse(executor.Request.BodyJson!).RootElement.GetProperty("email").GetString());
        Assert.Same(routes.UserCreate, executor.Request.Route);
    }

    [Fact(DisplayName = "Posts service resolves a user relationship route")]
    public async Task PostsServiceResolvesUserRelationship()
    {
        var executor = new RecordingExecutor();
        var routes = new ApiRoutes(new RouteSettings());

        await new PostsService(executor, routes).ListForUserAsync(
            42,
            authProfile: "anonymous",
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(ApiRequestConstants.Get, executor.Request!.Method);
        Assert.Equal($"{RouteSettings.DefaultUsersPath}/42/posts", executor.Request.Path);
        Assert.Equal(ApiRouteResources.Posts, executor.Request.Route!.Resource);
        Assert.Equal(ApiRouteActions.ListByUser, executor.Request.Route.Operation);
    }

    [Fact(DisplayName = "API module exposes domain steps while hiding composition details")]
    public async Task ApiModuleWiresStepsToExecutor()
    {
        var executor = new RecordingExecutor();
        var module = new ApiModule(executor, new ApiRoutes(new RouteSettings()));

        await module.Users.GetAsync(7, "anonymous", TestContext.Current.CancellationToken);

        Assert.Equal("/public/v2/users/7", executor.Request!.Path);
        Assert.Equal("users.item", executor.Request.Route!.Name);
        Assert.NotNull(module.Users);
        Assert.NotNull(module.Posts);
    }

    [Fact(DisplayName = "User data factory creates valid deterministic-looking test data")]
    public void UserDataFactoryCreatesValidData()
    {
        var first = UserDataFactory.Valid("Lifecycle / 2026");
        var second = UserDataFactory.Valid("Lifecycle / 2026");

        Assert.StartsWith("ATF User lifecycle202", first.Name, StringComparison.Ordinal);
        Assert.Contains("@example.com", first.Email, StringComparison.Ordinal);
        Assert.NotEqual(first.Email, second.Email);
        Assert.Equal("not-authorized@example.com", UserDataFactory.WithEmail("not-authorized@example.com").Email);
    }

    [Fact(DisplayName = "Fixture reader reads JSON from an explicit path")]
    public void FixtureReaderReadsJson()
    {
        var file = Path.Combine(Path.GetTempPath(), $"atf-fixture-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(file, "{\"Name\":\"fixture\",\"Email\":\"fixture@example.com\"}");
            var value = FixtureReader.Read<UserRequest>(file);

            Assert.Equal("fixture", value.Name);
            Assert.Equal("fixture@example.com", value.Email);
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Fact(DisplayName = "Fixture reader reports a missing fixture with the requested path")]
    public void FixtureReaderReportsMissingFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json");

        var exception = Assert.Throws<FileNotFoundException>(() => FixtureReader.ReadRaw(path));

        Assert.Equal(path, exception.FileName);
    }

    private sealed class RecordingExecutor : IApiExecutor
    {
        public ApiRequest? Request { get; private set; }

        public Task<ApiExecutionResult> ExecuteAsync(ApiRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            var response = new ApiResponse(HttpStatusCode.OK, "{}", new Dictionary<string, string>(), TimeSpan.Zero, "https://example.test");
            return Task.FromResult(new ApiExecutionResult(response, 1, TimeSpan.Zero, []));
        }
    }
}
