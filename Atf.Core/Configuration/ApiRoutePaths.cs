namespace Atf.Core.Configuration;

/// <summary>
/// Canonical relative API paths supported by the current API module.
///
/// These are defaults only. Runtime configuration may replace them through
/// RouteSettings without requiring a code change.
/// </summary>
public static class ApiRoutePaths
{
    public const string UsersCollection = "/public/v2/users";
    public const string PostsCollection = "/public/v2/posts";
    public const string UserPostsCollection = "/public/v2/users/{userId}/posts";
}
