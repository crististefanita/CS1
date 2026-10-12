using Atf.Core.Configuration;
using Atf.Core.Http;

namespace Atf.Common.Routing;

/// <summary>
/// Module-owned route catalog. It translates configurable relative paths into
/// typed route contracts consumed by services and semantic test steps.
/// </summary>
public sealed class ApiRoutes
{
    public ApiRoutes(RouteSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        UsersCollection = Route(
            ApiRouteNames.UsersCollection,
            ApiRequestMethods.Get,
            settings.Users,
            ApiRouteResources.Users,
            ApiRouteActions.List,
            AnonymousAccess());
        UserItem = Route(
            ApiRouteNames.UserItem,
            ApiRequestMethods.Get,
            ApiRoutePathTemplates.Item(settings.Users),
            ApiRouteResources.Users,
            ApiRouteActions.Get,
            AnonymousAccess());
        UserCreate = Route(
            ApiRouteNames.UserCreate,
            ApiRequestMethods.Post,
            settings.Users,
            ApiRouteResources.Users,
            ApiRouteActions.Create,
            DefaultBearerAccess());
        UserReplace = Route(
            ApiRouteNames.UserReplace,
            ApiRequestMethods.Put,
            ApiRoutePathTemplates.Item(settings.Users),
            ApiRouteResources.Users,
            ApiRouteActions.Replace,
            DefaultBearerAccess());
        UserPatch = Route(
            ApiRouteNames.UserPatch,
            ApiRequestMethods.Patch,
            ApiRoutePathTemplates.Item(settings.Users),
            ApiRouteResources.Users,
            ApiRouteActions.Patch,
            DefaultBearerAccess());
        UserDelete = Route(
            ApiRouteNames.UserDelete,
            ApiRequestMethods.Delete,
            ApiRoutePathTemplates.Item(settings.Users),
            ApiRouteResources.Users,
            ApiRouteActions.Delete,
            DefaultBearerAccess());

        PostsCollection = Route(
            ApiRouteNames.PostsCollection,
            ApiRequestMethods.Get,
            settings.Posts,
            ApiRouteResources.Posts,
            ApiRouteActions.List,
            AnonymousAccess());
        PostItem = Route(
            ApiRouteNames.PostItem,
            ApiRequestMethods.Get,
            ApiRoutePathTemplates.Item(settings.Posts),
            ApiRouteResources.Posts,
            ApiRouteActions.Get,
            AnonymousAccess());
        UserPostsCollection = Route(
            ApiRouteNames.UserPostsCollection,
            ApiRequestMethods.Get,
            settings.UserPosts,
            ApiRouteResources.Posts,
            ApiRouteActions.ListByUser,
            AnonymousAccess());
        UserPostCreate = Route(
            ApiRouteNames.UserPostCreate,
            ApiRequestMethods.Post,
            settings.UserPosts,
            ApiRouteResources.Posts,
            ApiRouteActions.CreateByUser,
            DefaultBearerAccess());

        All =
        [
            UsersCollection,
            UserItem,
            UserCreate,
            UserReplace,
            UserPatch,
            UserDelete,
            PostsCollection,
            PostItem,
            UserPostsCollection,
            UserPostCreate
        ];
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
    public IReadOnlyList<ApiRoute> All { get; }

    private static ApiRoute Route(
        string name,
        HttpMethod method,
        string pathTemplate,
        string resource,
        string operation,
        ApiRouteAuthentication authentication) =>
        new(name, method, pathTemplate, resource, operation, authentication);

    private static ApiRouteAuthentication AnonymousAccess() =>
        ApiRouteAuthentication.Anonymous(ApiRouteAuthProfiles.Anonymous);

    private static ApiRouteAuthentication DefaultBearerAccess() =>
        ApiRouteAuthentication.Required(ApiAuthType.Bearer, ApiRouteAuthProfiles.Default);
}
