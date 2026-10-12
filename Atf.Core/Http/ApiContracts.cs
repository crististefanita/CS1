using System.Net;

namespace Atf.Core.Http;

public sealed record ApiRequest(
    HttpMethod Method,
    string Path,
    string? BodyJson = null,
    IReadOnlyDictionary<string, string?>? Query = null,
    string? AuthProfile = null,
    ApiRoute? Route = null);

/// <summary>
/// Semantic route contract carried with an HTTP request so the report can explain
/// the configured endpoint, operation and authentication policy without exposing secrets.
/// </summary>
public sealed record ApiRoute(
    string Name,
    HttpMethod Method,
    string PathTemplate,
    string Resource,
    string Operation,
    ApiRouteAuthentication Authentication)
{
    public string DefaultAuthProfile => Authentication.Profile;
    public ApiAuthType AuthType => Authentication.Type;
    public ApiAuthRequirement AuthRequirement => Authentication.Requirement;
    public string AuthPolicy => Authentication.Policy;

    public string ResolvePath(params (string Token, object Value)[] values)
    {
        var path = PathTemplate;
        foreach (var (token, value) in values)
        {
            path = path.Replace(
                $"{{{token}}}",
                Uri.EscapeDataString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty),
                StringComparison.Ordinal);
        }

        if (path.Contains('{', StringComparison.Ordinal) || path.Contains('}', StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Route '{Name}' has unresolved path tokens: '{path}'.");
        }

        return path;
    }

    public ApiRequest CreateRequest(
        string? authProfile = null,
        string? bodyJson = null,
        IReadOnlyDictionary<string, string?>? query = null,
        params (string Token, object Value)[] values) =>
        new(Method, ResolvePath(values), bodyJson, query, authProfile ?? DefaultAuthProfile, this);
}

public sealed record RetryEvent(int Attempt, int StatusCode, string? Header, TimeSpan Delay, string Reason);

public sealed record ApiResponse(
    HttpStatusCode StatusCode,
    string Body,
    IReadOnlyDictionary<string, string> Headers,
    TimeSpan Duration,
    string Url)
{
    public int Status => (int)StatusCode;

    public string? Header(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Headers.TryGetValue(name, out var value))
        {
            return value;
        }

        // Http headers are case-insensitive by contract. The transport creates
        // a case-insensitive dictionary, but ApiResponse is public and can also
        // be constructed by a fake or a unit test with an ordinary dictionary.
        return Headers
            .FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            .Value;
    }
}

public sealed record ApiExecutionResult(
    ApiResponse Response,
    int Attempts,
    TimeSpan TotalDuration,
    IReadOnlyList<RetryEvent> RetryEvents);

public interface IApiExecutor
{
    Task<ApiExecutionResult> ExecuteAsync(ApiRequest request, CancellationToken cancellationToken = default);
}
