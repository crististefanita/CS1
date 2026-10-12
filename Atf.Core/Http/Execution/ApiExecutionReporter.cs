using Atf.Core.Http.Transport;
using Atf.Core.Reporting;

namespace Atf.Core.Http.Execution;

/// <summary>
/// Projects one HTTP exchange onto the reporting port.
///
/// Reporting is diagnostic infrastructure: a broken renderer must not change
/// the transport result, retry decision or exception observed by the caller.
/// Keeping that policy in one adapter prevents <see cref="ApiExecutor"/> from
/// becoming a mixture of HTTP orchestration and presentation plumbing.
/// </summary>
internal sealed class ApiExecutionReporter
{
    private readonly IAtfReporter _reporter;

    public ApiExecutionReporter(IAtfReporter reporter)
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    }

    public void Start(ApiRequest request, string url, string authProfile, IApiAuthenticator authenticator)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(authenticator);

        Try(() => _reporter.StartStep(Title(request)));

        if (request.Route is not null)
        {
            Detail("route.name", request.Route.Name);
            Detail("route.resource", request.Route.Resource);
            Detail("route.operation", request.Route.Operation);
            Detail("route.path.template", request.Route.PathTemplate);
            Detail("route.auth.type", request.Route.Authentication.TypeName);
            Detail("route.auth.requirement", request.Route.Authentication.Requirement.ToString().ToLowerInvariant());
            Detail("route.auth.profile", request.Route.Authentication.Profile);
            Detail("route.auth.policy", request.Route.AuthPolicy);
        }

        Detail("request.type", $"HTTP {request.Method.Method.ToUpperInvariant()}");
        Detail("method", request.Method.Method.ToUpperInvariant());
        Detail("path", request.Path);
        Detail("url", url);
        Detail("auth.profile", authProfile);
        Detail("auth.mode", authenticator.DescribeMode(authProfile));
        Detail("auth.token.state", authenticator.DescribeTokenState(authProfile));

        if (!string.IsNullOrWhiteSpace(request.BodyJson))
        {
            Json("request.body", JsonFormatter.Pretty(request.BodyJson));
        }
    }

    public void Attempt(int attempt) => Detail("attempt", (attempt + 1).ToString());

    public void Response(ApiResponse response, TimeSpan exchangeDuration)
    {
        ArgumentNullException.ThrowIfNull(response);

        Detail("response.status", response.Status.ToString());
        var contentType = response.Header("Content-Type");
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            Detail("response.content.type", contentType);
        }

        Detail("response.duration.ms", response.Duration.TotalMilliseconds.ToString("F0"));
        Detail("exchange.duration.ms", exchangeDuration.TotalMilliseconds.ToString("F0"));
        Json("response.body", JsonFormatter.Pretty(response.Body));
    }

    public void Retry(RetryEvent retryEvent)
    {
        ArgumentNullException.ThrowIfNull(retryEvent);

        Detail("rate-limit.retry.delay.ms", retryEvent.Delay.TotalMilliseconds.ToString("F0"));
        Detail("rate-limit.retry.reason", retryEvent.Reason);
    }

    /// <summary>
    /// Ends the request-owned evidence node before the caller starts the
    /// response-owned validation group. This lifecycle boundary prevents a
    /// validation table from becoming a child of GET/POST.
    /// </summary>
    public void Completed() => Try(() =>
    {
        _reporter.Info("HTTP exchange completed; contract validation pending.");
        _reporter.CompleteStep(AtfReportStatus.Info);
    });

    public void Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Try(() => _reporter.Fail("HTTP exchange failed.", exception));
        Try(() => _reporter.CompleteStep(AtfReportStatus.Fail));
    }

    private void Detail(string key, string value) => Try(() => _reporter.Detail(key, value));

    private void Json(string label, string json) => Try(() => _reporter.Json(label, json));

    private static string Title(ApiRequest request) =>
        $"{request.Method.Method.ToUpperInvariant()} {request.Path}";

    private static void Try(Action action)
    {
        try
        {
            action();
        }
        catch
        {
            // The report is evidence, not the execution authority. Deliberately
            // keep the HTTP outcome independent from a renderer failure.
        }
    }
}
