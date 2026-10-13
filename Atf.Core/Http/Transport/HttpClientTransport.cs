using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace Atf.Core.Http.Transport;

public sealed class HttpClientTransport : IApiTransport
{
    private readonly HttpClient _client;
    private readonly IApiAuthenticator _authenticator;

    public HttpClientTransport(HttpClient client, IApiAuthenticator authenticator)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
    }

    public async Task<ApiResponse> SendAsync(
        ApiRequest request,
        string url,
        string authProfile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = new HttpRequestMessage(request.Method, url);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _authenticator.Apply(message, authProfile);

        if (!string.IsNullOrWhiteSpace(request.BodyJson))
        {
            message.Content = new StringContent(request.BodyJson, Encoding.UTF8, "application/json");
        }

        var started = Stopwatch.GetTimestamp();
        using var response = await _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var headers = response.Headers
            .Concat(response.Content.Headers)
            .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => string.Join(", ", group.SelectMany(pair => pair.Value)),
                StringComparer.OrdinalIgnoreCase);

        return new(response.StatusCode, body, headers, Stopwatch.GetElapsedTime(started), url);
    }
}
