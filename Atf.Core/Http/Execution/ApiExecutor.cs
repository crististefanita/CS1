using Atf.Core.Configuration;
using Atf.Core.Reporting;
using Atf.Core.Resilience;
using Atf.Core.Security;
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
            null,
            null,
            null)
    {
    }

    public ApiExecutor(
        HttpClient client,
        ApiSettings settings,
        IAtfReporter reporter,
        IApiAuthenticator? authenticator,
        IApiTransport? transport = null,
        IApiUrlBuilder? urlBuilder = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        var snapshot = settings?.Snapshot() ?? throw new ArgumentNullException(nameof(settings));
        _settings = snapshot;
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _authenticator = authenticator ?? new ConfiguredApiAuthenticator(snapshot);
        _transport = transport ?? new HttpClientTransport(client, _authenticator);
        _urlBuilder = urlBuilder ?? new ApiUrlBuilder(snapshot);
        _retryPolicy = new RateLimitRetryPolicy(snapshot.RateLimit);
        _client.Timeout = TimeSpan.FromSeconds(snapshot.TimeoutSeconds);
    }

    public async Task<ApiExecutionResult> ExecuteAsync(ApiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var authProfile = request.AuthProfile ?? _settings.DefaultAuthProfile;
        var report = new ApiExecutionReporter(_reporter, new SensitiveDataRedactor(_settings.Redaction));
        var retryEvents = new List<RetryEvent>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var url = _urlBuilder.Build(request);
            report.Start(request, url, authProfile, _authenticator);

            for (var attempt = 0; ; attempt++)
            {
                report.Attempt(attempt);
                var response = await _transport.SendAsync(request, url, authProfile, cancellationToken);

                var decision = _retryPolicy.Decide(response, attempt);
                if (!decision.ShouldRetry)
                {
                    stopwatch.Stop();
                }

                report.Response(response, stopwatch.Elapsed);

                if (!decision.ShouldRetry)
                {
                    report.Completed();
                    return new(response, attempt + 1, stopwatch.Elapsed, retryEvents);
                }

                var retryEvent = new RetryEvent(attempt + 1, response.Status, decision.Header, decision.Delay, decision.Reason);
                retryEvents.Add(retryEvent);
                report.Retry(retryEvent);
                await Task.Delay(decision.Delay, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            // Reporting is diagnostic infrastructure. A faulty reporter must
            // never replace the transport/authentication exception that the
            // caller needs to diagnose.
            report.Failed(exception);
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
