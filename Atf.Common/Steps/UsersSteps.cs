using Atf.Common.Models;
using Atf.Common.Services;
using Atf.Core.Http;

namespace Atf.Common.Steps;

public sealed class UsersSteps(UsersService service)
{
    public Task<ApiExecutionResult> CreateAsync(UserRequest request, string? authProfile = null, CancellationToken ct = default) =>
        service.CreateAsync(request, authProfile, ct);

    public Task<ApiExecutionResult> GetAsync(int id, string? authProfile = null, CancellationToken ct = default) =>
        service.GetAsync(id, authProfile, ct);

    public Task<ApiExecutionResult> ListAsync(IReadOnlyDictionary<string, string?>? query = null, string? authProfile = null, CancellationToken ct = default) =>
        service.ListAsync(query, authProfile, ct);

    public Task<ApiExecutionResult> PatchAsync(int id, UserRequest request, string? authProfile = null, CancellationToken ct = default) =>
        service.PatchAsync(id, request, authProfile, ct);

    public Task<ApiExecutionResult> DeleteAsync(int id, string? authProfile = null, CancellationToken ct = default) =>
        service.DeleteAsync(id, authProfile, ct);
}
