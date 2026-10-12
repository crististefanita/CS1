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

        Assert.Equal(HttpMethod.Post, routes.UserCreate.Method);
        Assert.Equal("/public/v2/users", routes.UserCreate.PathTemplate);
        Assert.Equal("default", routes.UserCreate.DefaultAuthProfile);
        Assert.Equal(ApiAuthType.Bearer, routes.UserCreate.AuthType);
        Assert.Equal(ApiAuthRequirement.Required, routes.UserCreate.AuthRequirement);
        Assert.Equal("bearer required", routes.UserCreate.AuthPolicy);

        Assert.Equal(HttpMethod.Get, routes.UserPostsCollection.Method);
        Assert.Equal("anonymous", routes.UserPostsCollection.DefaultAuthProfile);
        Assert.Equal(ApiAuthType.None, routes.UserPostsCollection.AuthType);
        Assert.Equal(ApiAuthRequirement.Optional, routes.UserPostsCollection.AuthRequirement);
        Assert.Equal("/public/v2/users/{userId}/posts", routes.UserPostsCollection.PathTemplate);
    }

    [Fact(DisplayName = "Route request carries resolved path and route metadata into executor")]
    public void RequestResolvesTemplateAndCarriesMetadata()
    {
        var route = new ApiRoutes(new RouteSettings()).UserPostsCollection;
        var request = route.CreateRequest(values: [("userId", 8663020)]);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/public/v2/users/8663020/posts", request.Path);
        Assert.Equal("anonymous", request.AuthProfile);
        Assert.Same(route, request.Route);
        Assert.Equal("users.posts.collection", request.Route?.Name);
    }
}
