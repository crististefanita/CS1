using System.Net.Http;

namespace Atf.Common.Routing;

/// <summary>
/// Stable identifiers used to explain a route in logs, evidence and reports.
/// They are not URLs and must not be used to build requests directly.
/// </summary>
public static class ApiRouteNames
{
    public const string UsersCollection = "users.collection";
    public const string UserItem = "users.item";
    public const string UserCreate = "users.create";
    public const string UserReplace = "users.replace";
    public const string UserPatch = "users.patch";
    public const string UserDelete = "users.delete";
    public const string PostsCollection = "posts.collection";
    public const string PostItem = "posts.item";
    public const string UserPostsCollection = "users.posts.collection";
    public const string UserPostCreate = "users.posts.create";
}

/// <summary>
/// Resource vocabulary used by route metadata and report grouping.
/// </summary>
public static class ApiRouteResources
{
    public const string Users = "users";
    public const string Posts = "posts";
}

/// <summary>
/// Semantic operation vocabulary. These values intentionally remain strings
/// because they are rendered as readable evidence and can be extended by a module.
/// </summary>
public static class ApiRouteActions
{
    public const string List = "list";
    public const string Get = "get";
    public const string Create = "create";
    public const string Replace = "replace";
    public const string Patch = "patch";
    public const string Delete = "delete";
    public const string ListByUser = "list by user";
    public const string CreateByUser = "create by user";
}

/// <summary>
/// Path token names used by route templates and ApiRoute.ResolvePath.
/// </summary>
public static class ApiRouteTokens
{
    public const string Id = "id";
    public const string UserId = "userId";
}

/// <summary>
/// Authentication profile names referenced by the route catalog.
/// The profile contents remain configuration-owned.
/// </summary>
public static class ApiRouteAuthProfiles
{
    public const string Anonymous = "anonymous";
    public const string Default = "default";
}

/// <summary>
/// HTTP methods allowed by the current API route catalog.
/// HttpMethod is a reference type, therefore these are readonly values rather
/// than C# const fields.
/// </summary>
public static class ApiRequestMethods
{
    public static readonly HttpMethod Get = HttpMethod.Get;
    public static readonly HttpMethod Post = HttpMethod.Post;
    public static readonly HttpMethod Put = HttpMethod.Put;
    public static readonly HttpMethod Patch = HttpMethod.Patch;
    public static readonly HttpMethod Delete = HttpMethod.Delete;
}

/// <summary>
/// Small path-template builders keep token syntax in one place while allowing
/// the collection roots to remain environment/module configurable.
/// </summary>
public static class ApiRoutePathTemplates
{
    public static string Item(string collectionPath) =>
        $"{Normalize(collectionPath)}/{{{ApiRouteTokens.Id}}}";

    private static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return path.TrimEnd('/');
    }
}
