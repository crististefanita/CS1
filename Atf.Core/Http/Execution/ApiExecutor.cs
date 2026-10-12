using Atf.Core.Configuration;
using Atf.Core.Reporting;
using Atf.Core.Resilience;
using System.Diagnostics;

namespace Atf.Core.Http.Execution;

public sealed class ApiExecutor : IApiExecutor
{
    private readonly HttpClient _client;
    private readonly ApiSettings _settings;
    private readonly IAtfReporter _reporter;
    private readonly RateLimitRetryPolicy _retryPolicy;
    private readonly IApiTransport _transport;
    private readonly IApiAuthenticator _authenticator;
    private readonly IApiUrlBuilder _urlBuilder;

    public ApiExecutor(HttpClient client, ApiSettings settings, IAtfReporter reporter)
        : this(
            client,
            settings,
            reporter,
            new ConfiguredApiAuthenticator(settings),
            null,
            null)
    {
    }

    public ApiExecutor(
        HttpClient client,
        ApiSettings settings,
        IAtfReporter reporter,
        IApiAuthenticator authenticator,
        IApiTransport? transport = null,
        IApiUrlBuilder? urlBuilder = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        _transport = transport ?? new HttpClientTransport(client, authenticator);
        _urlBuilder = urlBuilder ?? new ApiUrlBuilder(settings);
        _retryPolicy = new RateLimitRetryPolicy(settings.RateLimit);
        _client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    }

    public async Task<ApiExecutionResult> ExecuteAsync(ApiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = _urlBuilder.Build(request);
        var title = $"{request.Method.Method.ToUpperInvariant()} {request.Path}";
        var authProfile = request.AuthProfile ?? _settings.DefaultAuthProfile;
        _reporter.StartStep(title);
        if (request.Route is not null)
        {
            _reporter.Detail("route.name", request.Route.Name);
            _reporter.Detail("route.resource", request.Route.Resource);
            _reporter.Detail("route.operation", request.Route.Operation);
            _reporter.Detail("route.path.template", request.Route.PathTemplate);
            _reporter.Detail("route.auth.type", request.Route.Authentication.TypeName);
            _reporter.Detail("route.auth.requirement", request.Route.Authentication.Requirement.ToString().ToLowerInvariant());
            _reporter.Detail("route.auth.profile", request.Route.Authentication.Profile);
            _reporter.Detail("route.auth.policy", request.Route.AuthPolicy);
        }
        _reporter.Detail("request.type", $"HTTP {request.Method.Method.ToUpperInvariant()}");
        _reporter.Detail("method", request.Method.Method.ToUpperInvariant());
        _reporter.Detail("path", request.Path);
        _reporter.Detail("url", url);
        _reporter.Detail("auth.profile", authProfile);
        _reporter.Detail("auth.mode", _authenticator.DescribeMode(authProfile));
        _reporter.Detail("auth.token.state", _authenticator.DescribeTokenState(authProfile));
        if (!string.IsNullOrWhiteSpace(request.BodyJson))
        {
            _reporter.Json("request.body", JsonFormatter.Pretty(request.BodyJson));
        }

        var retryEvents = new List<RetryEvent>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            for (var attempt = 0; ; attempt++)
            {
                _reporter.Detail("attempt", (attempt + 1).ToString());
                var response = await _transport.SendAsync(request, url, authProfile, cancellationToken);
                _reporter.Detail("response.status", response.Status.ToString());
                if (response.Headers.TryGetValue("Content-Type", out var contentType))
                {
                    _reporter.Detail("response.content.type", contentType);
                }
                _reporter.Detail("response.duration.ms", response.Duration.TotalMilliseconds.ToString("F0"));

                var decision = _retryPolicy.Decide(response, attempt);
                if (!decision.ShouldRetry)
                {
                    stopwatch.Stop();
                    _reporter.Detail("exchange.duration.ms", stopwatch.Elapsed.TotalMilliseconds.ToString("F0"));
                }

                _reporter.Json("response.body", JsonFormatter.Pretty(response.Body));

                if (!decision.ShouldRetry)
                {
                    _reporter.Info("HTTP exchange completed; contract validation pending.");
                    return new(response, attempt + 1, stopwatch.Elapsed, retryEvents);
                }

                var retryEvent = new RetryEvent(attempt + 1, response.Status, decision.Header, decision.Delay, decision.Reason);
                retryEvents.Add(retryEvent);
                _reporter.Detail("rate-limit.retry.delay.ms", decision.Delay.TotalMilliseconds.ToString("F0"));
                _reporter.Detail("rate-limit.retry.reason", decision.Reason);
                await Task.Delay(decision.Delay, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            // Reporting is diagnostic infrastructure. A faulty reporter must
            // never replace the transport/authentication exception that the
            // caller needs to diagnose.
            try
            {
                _reporter.Fail("HTTP exchange failed.", exception);
            }
            catch
            {
                // Preserve the original exception and stack trace.
            }

            throw;
        }
    }

}

public static class JsonFormatter
{
    public static string Pretty(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "<empty>";
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            return System.Text.Json.JsonSerializer.Serialize(document.RootElement, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (System.Text.Json.JsonException)
        {
            return json;
        }
    }
}
