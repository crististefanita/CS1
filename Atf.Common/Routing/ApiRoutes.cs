using Atf.Core.Configuration;
using Atf.Core.Http;

namespace Atf.Common.Routing;

/// <summary>
/// API route catalog. Path templates remain configurable through RouteSettings;
/// this class adds the executable contract used by services and reporting.
/// </summary>
public sealed class ApiRoutes
{
    public ApiRoutes(RouteSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        UsersCollection = Route("users.collection", HttpMethod.Get, settings.Users, "users", "list", ApiRouteAuthentication.Anonymous());
        UserItem = Route("users.item", HttpMethod.Get, $"{settings.Users}/{{id}}", "users", "get", ApiRouteAuthentication.Anonymous());
        UserCreate = Route("users.create", HttpMethod.Post, settings.Users, "users", "create", ApiRouteAuthentication.Required(ApiAuthType.Bearer, "default"));
        UserReplace = Route("users.replace", HttpMethod.Put, $"{settings.Users}/{{id}}", "users", "replace", ApiRouteAuthentication.Required(ApiAuthType.Bearer, "default"));
        UserPatch = Route("users.patch", HttpMethod.Patch, $"{settings.Users}/{{id}}", "users", "patch", ApiRouteAuthentication.Required(ApiAuthType.Bearer, "default"));
        UserDelete = Route("users.delete", HttpMethod.Delete, $"{settings.Users}/{{id}}", "users", "delete", ApiRouteAuthentication.Required(ApiAuthType.Bearer, "default"));

        PostsCollection = Route("posts.collection", HttpMethod.Get, settings.Posts, "posts", "list", ApiRouteAuthentication.Anonymous());
        PostItem = Route("posts.item", HttpMethod.Get, $"{settings.Posts}/{{id}}", "posts", "get", ApiRouteAuthentication.Anonymous());
        UserPostsCollection = Route("users.posts.collection", HttpMethod.Get, settings.UserPosts, "posts", "list by user", ApiRouteAuthentication.Anonymous());
        UserPostCreate = Route("users.posts.create", HttpMethod.Post, settings.UserPosts, "posts", "create by user", ApiRouteAuthentication.Required(ApiAuthType.Bearer, "default"));
    }

    public ApiRoute UsersCollection { get; }
    public ApiRoute UserItem { get; }
    public ApiRoute UserCreate { get; }
    public ApiRoute UserReplace { get; }
    public ApiRoute UserPatch { get; }
    public ApiRoute UserDelete { get; }
    public ApiRoute PostsCollection { get; }
    public ApiRoute PostItem { get; }
    public ApiRoute UserPostsCollection { get; }
    public ApiRoute UserPostCreate { get; }

    private static ApiRoute Route(
        string name,
        HttpMethod method,
        string pathTemplate,
        string resource,
        string operation,
        ApiRouteAuthentication authentication) =>
        new(name, method, pathTemplate, resource, operation, authentication);
}
