using Atf.Common.Models;
using Atf.Common.Services;
using Atf.Core.Http;

namespace Atf.Common.Steps;

public sealed class PostsSteps(PostsService service)
{
    public Task<ApiExecutionResult> ListAsync(IReadOnlyDictionary<string, string?>? query = null, string? authProfile = null, CancellationToken ct = default) =>
        service.ListAsync(query, authProfile, ct);

    public Task<ApiExecutionResult> GetAsync(int id, string? authProfile = null, CancellationToken ct = default) =>
        service.GetAsync(id, authProfile, ct);

    public Task<ApiExecutionResult> ListForUserAsync(int userId, IReadOnlyDictionary<string, string?>? query = null, string? authProfile = null, CancellationToken ct = default) =>
        service.ListForUserAsync(userId, query, authProfile, ct);

    public Task<ApiExecutionResult> CreateForUserAsync(int userId, PostRequest request, string? authProfile = null, CancellationToken ct = default) =>
        service.CreateForUserAsync(userId, request, authProfile, ct);
}
