using Atf.Common.Routing;
using Atf.Core.Configuration;
using Atf.Core.Http;
using System.Net;
using Xunit;

namespace Atf.Api;

public sealed class ApiRoutesTests
{
    [Fact(DisplayName = "Route catalog preserves configured paths and semantic auth policies")]
    public void CatalogDescribesConfiguredApiContract()
    {
        var routes = new ApiRoutes(new RouteSettings
        {
            Users = "/public/v2/users",
            Posts = "/public/v2/posts",
            UserPosts = "/public/v2/users/{userId}/posts"
        });

        Assert.Equal(ApiRequestMethods.Post, routes.UserCreate.Method);
        Assert.Equal(RouteSettings.DefaultUsersPath, routes.UserCreate.PathTemplate);
        Assert.Equal(ApiRouteAuthProfiles.Default, routes.UserCreate.DefaultAuthProfile);
        Assert.Equal(ApiAuthType.Bearer, routes.UserCreate.AuthType);
        Assert.Equal(ApiAuthRequirement.Required, routes.UserCreate.AuthRequirement);
        Assert.Equal("bearer required", routes.UserCreate.AuthPolicy);

        Assert.Equal(ApiRequestMethods.Get, routes.UserPostsCollection.Method);
        Assert.Equal(ApiRouteAuthProfiles.Anonymous, routes.UserPostsCollection.DefaultAuthProfile);
        Assert.Equal(ApiAuthType.None, routes.UserPostsCollection.AuthType);
        Assert.Equal(ApiAuthRequirement.Optional, routes.UserPostsCollection.AuthRequirement);
        Assert.Equal(RouteSettings.DefaultUserPostsPath, routes.UserPostsCollection.PathTemplate);
        Assert.Equal(ApiRouteActions.ListByUser, routes.UserPostsCollection.Operation);
        Assert.Equal(ApiRouteNames.UserPostsCollection, routes.UserPostsCollection.Name);
    }

    [Fact(DisplayName = "Route request carries resolved path and route metadata into executor")]
    public void RequestResolvesTemplateAndCarriesMetadata()
    {
        var route = new ApiRoutes(new RouteSettings()).UserPostsCollection;
        var request = route.CreateRequest(values: [(ApiRouteTokens.UserId, 8663020)]);

        Assert.Equal(ApiRequestMethods.Get, request.Method);
        Assert.Equal("/public/v2/users/8663020/posts", request.Path);
        Assert.Equal(ApiRouteAuthProfiles.Anonymous, request.AuthProfile);
        Assert.Same(route, request.Route);
        Assert.Equal(ApiRouteNames.UserPostsCollection, request.Route?.Name);
    }

    [Fact(DisplayName = "Route catalog exposes one deterministic inventory without duplicates")]
    public void CatalogExposesDeterministicInventory()
    {
        var routes = new ApiRoutes(new RouteSettings());

        Assert.Equal(10, routes.All.Count);
        Assert.Equal(routes.All.Count, routes.All.Select(route => route.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(routes.All, route =>
        {
            Assert.False(string.IsNullOrWhiteSpace(route.PathTemplate));
            Assert.False(string.IsNullOrWhiteSpace(route.Resource));
            Assert.False(string.IsNullOrWhiteSpace(route.Operation));
        });
    }

    [Fact(DisplayName = "Item path templates normalize configurable collection slashes")]
    public void ItemPathTemplateNormalizesTrailingSlash()
    {
        Assert.Equal("/v3/users/{id}", ApiRoutePathTemplates.Item("/v3/users/"));
    }
}
