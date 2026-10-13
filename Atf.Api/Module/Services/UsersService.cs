using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Common.Data;
using Atf.Common.Models;
using Atf.Common.Routing;
using System.Text.Json;

namespace Atf.Common.Services;

public sealed class UsersService(IApiExecutor executor, ApiRoutes routes)
{
    public Task<ApiExecutionResult> CreateAsync(UserRequest request, string? authProfile = null, CancellationToken ct = default) =>
        SendAsync(routes.UserCreate, request, authProfile, ct);

    public Task<ApiExecutionResult> GetAsync(int id, string? authProfile = null, CancellationToken ct = default) =>
        executor.ExecuteAsync(routes.UserItem.CreateRequest(authProfile, values: [(ApiRouteTokens.Id, id)]), ct);

    public Task<ApiExecutionResult> ListAsync(IReadOnlyDictionary<string, string?>? query = null, string? authProfile = null, CancellationToken ct = default) =>
        executor.ExecuteAsync(routes.UsersCollection.CreateRequest(authProfile, query: query), ct);

    public Task<ApiExecutionResult> ReplaceAsync(int id, UserRequest request, string? authProfile = null, CancellationToken ct = default) =>
        SendAsync(routes.UserReplace, request, authProfile, ct, [(ApiRouteTokens.Id, id)]);

    public Task<ApiExecutionResult> PatchAsync(int id, UserRequest request, string? authProfile = null, CancellationToken ct = default) =>
        SendAsync(routes.UserPatch, request, authProfile, ct, [(ApiRouteTokens.Id, id)]);

    public Task<ApiExecutionResult> DeleteAsync(int id, string? authProfile = null, CancellationToken ct = default) =>
        executor.ExecuteAsync(routes.UserDelete.CreateRequest(authProfile, values: [(ApiRouteTokens.Id, id)]), ct);

    private Task<ApiExecutionResult> SendAsync(
        ApiRoute route,
        UserRequest body,
        string? authProfile,
        CancellationToken ct,
        params (string Token, object Value)[] values)
    {
        var json = AtfJsonOptions.SerializeWeb(body);
        return executor.ExecuteAsync(route.CreateRequest(authProfile, json, values: values), ct);
    }
}
