using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Common.Data;
using Atf.Common.Models;
using Atf.Common.Routing;
using System.Text.Json;

namespace Atf.Common.Services;

public sealed class PostsService(IApiExecutor executor, ApiRoutes routes)
{
    public Task<ApiExecutionResult> ListAsync(IReadOnlyDictionary<string, string?>? query = null, string? authProfile = null, CancellationToken ct = default) =>
        executor.ExecuteAsync(routes.PostsCollection.CreateRequest(authProfile, query: query), ct);

    public Task<ApiExecutionResult> GetAsync(int id, string? authProfile = null, CancellationToken ct = default) =>
        executor.ExecuteAsync(routes.PostItem.CreateRequest(authProfile, values: [(ApiRouteTokens.Id, id)]), ct);

    public Task<ApiExecutionResult> ListForUserAsync(int userId, IReadOnlyDictionary<string, string?>? query = null, string? authProfile = null, CancellationToken ct = default) =>
        executor.ExecuteAsync(routes.UserPostsCollection.CreateRequest(authProfile, query: query, values: [(ApiRouteTokens.UserId, userId)]), ct);

    public Task<ApiExecutionResult> CreateForUserAsync(int userId, PostRequest request, string? authProfile = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(request, AtfJsonOptions.Web);
        return executor.ExecuteAsync(routes.UserPostCreate.CreateRequest(authProfile, json, values: [(ApiRouteTokens.UserId, userId)]), ct);
    }
}
