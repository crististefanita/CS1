using Atf.Core.Configuration;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace Atf.Core.Http.Transport;

/// <summary>
/// The transport boundary keeps network I/O out of the execution/orchestration
/// logic. Production uses <see cref="HttpClientTransport"/>; unit tests can use a
/// deterministic fake without a socket or a live API.
/// </summary>
public interface IApiTransport
{
    Task<ApiResponse> SendAsync(
        ApiRequest request,
        string url,
        string authProfile,
        CancellationToken cancellationToken = default);
}

public interface IApiAuthenticator
{
    void Apply(HttpRequestMessage message, string profileName);
    string DescribeMode(string profileName);
    string DescribeTokenState(string profileName);
}

public sealed class ConfiguredApiAuthenticator : IApiAuthenticator
{
    private readonly ApiSettings _settings;
    private readonly Func<string, string?> _tokenReader;

    public ConfiguredApiAuthenticator(
        ApiSettings settings,
        Func<string, string?>? tokenReader = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _tokenReader = tokenReader ?? Environment.GetEnvironmentVariable;
    }

    public void Apply(HttpRequestMessage message, string profileName)
    {
        var profile = GetProfile(profileName);
        if (string.Equals(profile.Type, "none", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(profile.Type, "bearer", StringComparison.OrdinalIgnoreCase))
        {
            var token = _tokenReader(TokenVariable(profile));
            if (!string.IsNullOrWhiteSpace(token))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return;
        }

        if (string.Equals(profile.Type, "apiKey", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(profile.Type, "apikey", StringComparison.OrdinalIgnoreCase))
        {
            var key = _tokenReader(TokenVariable(profile));
            if (!string.IsNullOrWhiteSpace(key))
            {
                var header = string.IsNullOrWhiteSpace(profile.HeaderName) ? "X-API-Key" : profile.HeaderName;
                message.Headers.TryAddWithoutValidation(header, key);
            }

            return;
        }

        if (string.Equals(profile.Type, "basic", StringComparison.OrdinalIgnoreCase))
        {
            var username = _tokenReader(profile.UsernameEnvironmentVariable ?? "ATF_API_USERNAME");
            var password = _tokenReader(profile.PasswordEnvironmentVariable ?? "ATF_API_PASSWORD");
            if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
            {
                var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                message.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
            }

            return;
        }

        if (string.Equals(profile.Type, "custom", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var header in profile.Headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Key) && header.Value is not null)
                {
                    message.Headers.TryAddWithoutValidation(header.Key, ResolveConfiguredValue(header.Value));
                }
            }

            return;
        }

        throw new InvalidOperationException($"Unsupported auth profile type '{profile.Type}'.");
    }

    public string DescribeMode(string profileName) => GetProfile(profileName).Type;

    public string DescribeTokenState(string profileName)
    {
        var profile = GetProfile(profileName);
        if (string.Equals(profile.Type, "none", StringComparison.OrdinalIgnoreCase))
        {
            return "not applicable";
        }

        if (string.Equals(profile.Type, "bearer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(profile.Type, "apiKey", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(profile.Type, "apikey", StringComparison.OrdinalIgnoreCase))
        {
            var variable = TokenVariable(profile);
            return string.IsNullOrWhiteSpace(_tokenReader(variable))
                ? $"missing ({variable})"
                : string.Equals(profile.Type, "bearer", StringComparison.OrdinalIgnoreCase)
                    ? "bearer token available"
                    : "api key available";
        }

        if (string.Equals(profile.Type, "basic", StringComparison.OrdinalIgnoreCase))
        {
            var username = profile.UsernameEnvironmentVariable ?? "ATF_API_USERNAME";
            var password = profile.PasswordEnvironmentVariable ?? "ATF_API_PASSWORD";
            return string.IsNullOrWhiteSpace(_tokenReader(username)) || string.IsNullOrWhiteSpace(_tokenReader(password))
                ? $"missing ({username}/{password})"
                : "basic credentials available";
        }

        if (string.Equals(profile.Type, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return profile.Headers.Count == 0 ? "custom headers not configured" : "custom headers configured";
        }

        return "unsupported profile type";
    }

    private AuthProfileSettings GetProfile(string profileName) =>
        _settings.AuthProfiles.TryGetValue(profileName, out var profile)
            ? profile
            : throw new InvalidOperationException($"Auth profile '{profileName}' is not configured.");

    private string TokenVariable(AuthProfileSettings profile) =>
        string.IsNullOrWhiteSpace(profile.TokenEnvironmentVariable)
            ? _settings.TokenEnvironmentVariable
            : profile.TokenEnvironmentVariable;

    private string ResolveConfiguredValue(string value)
    {
        if (value.StartsWith("${", StringComparison.Ordinal) && value.EndsWith('}'))
        {
            return _tokenReader(value[2..^1]) ?? string.Empty;
        }

        return value;
    }
}

public interface IApiUrlBuilder
{
    string Build(ApiRequest request);
}

public sealed class ApiUrlBuilder(ApiSettings settings) : IApiUrlBuilder
{
    private readonly ApiSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public string Build(ApiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var baseUri = _settings.BaseUrl.TrimEnd('/') + "/";
        var relative = request.Path.TrimStart('/');
        var builder = new UriBuilder(new Uri(new Uri(baseUri), relative));
        if (request.Query is { Count: > 0 })
        {
            builder.Query = string.Join("&", request.Query
                .Where(item => item.Value is not null)
                .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}"));
        }

        // AbsoluteUri preserves escaping for query values (UriBuilder.ToString()
        // normalizes some escaped spaces back to literal spaces).
        return builder.Uri.AbsoluteUri;
    }
}

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
