namespace Atf.Core.Http.Transport;

/// <summary>
/// Transport boundary for network I/O. Execution/orchestration depends on
/// this port and can therefore use deterministic fakes in offline tests.
/// </summary>
public interface IApiTransport
{
    Task<ApiResponse> SendAsync(
        ApiRequest request,
        string url,
        string authProfile,
        CancellationToken cancellationToken = default);
}
