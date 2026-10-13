namespace Atf.Core.Configuration.Models;

public sealed class RouteSettings
{
    public const string DefaultUsersPath = ApiRoutePaths.UsersCollection;
    public const string DefaultPostsPath = ApiRoutePaths.PostsCollection;
    public const string DefaultUserPostsPath = ApiRoutePaths.UserPostsCollection;

    public string Users { get; init; } = DefaultUsersPath;
    public string Posts { get; init; } = DefaultPostsPath;
    public string UserPosts { get; init; } = DefaultUserPostsPath;

    public RouteSettings Snapshot() => new()
    {
        Users = Users,
        Posts = Posts,
        UserPosts = UserPosts
    };
}
