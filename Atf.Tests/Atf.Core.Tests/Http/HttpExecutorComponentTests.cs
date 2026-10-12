using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;

namespace Atf.Core.Tests.Http;

public sealed class HttpExecutorComponentTests
{
    [Fact(DisplayName = "Executor retries rate limiting and returns the eventual response")]
    public async Task ExecutorRetriesRateLimitedResponse()
    {
        var reporter = new RecordingReporter();
        var transport = new SequenceTransport(
            Response(HttpStatusCode.TooManyRequests, "{\"error\":\"busy\"}",
                new Dictionary<string, string> { ["Retry-After"] = "1" }),
            Response(HttpStatusCode.OK, "{\"ok\":true}"));
        var executor = CreateExecutor(reporter, transport, new RateLimitSettings
        {
            MaxRetries = 1,
            DefaultDelaySeconds = 1,
            MaxDelaySeconds = 1,
            RetryStatuses = [429],
            DelayHeaders = ["Retry-After"]
        });

        var result = await executor.ExecuteAsync(new ApiRequest(HttpMethod.Get, "/items", AuthProfile: "anonymous"));

        Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
        Assert.Equal(2, result.Attempts);
        Assert.Single(result.RetryEvents);
        Assert.Equal(TimeSpan.FromSeconds(1), result.RetryEvents[0].Delay);
        Assert.Equal(2, transport.Calls);
        Assert.Equal("2", reporter.Details["attempt"]);
        Assert.Equal("429", reporter.Details["attempt.1.response.status"]);
        Assert.Equal("429", reporter.Details["attempt.1.retry.status"]);
        Assert.Equal("200", reporter.Details["attempt.2.response.status"]);
        Assert.Contains("rate-limit.retry.reason", reporter.Details.Keys);
        Assert.Contains(reporter.Infos, info => info.Contains("Attempt 1", StringComparison.Ordinal));
        Assert.Contains(reporter.Infos, info => info.Contains("HTTP 429", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Executor returns a single non-retryable response without a retry event")]
    public async Task ExecutorDoesNotRetryNonRetryableResponse()
    {
        var transport = new SequenceTransport(Response(HttpStatusCode.BadRequest, "{\"error\":\"invalid\"}"));
        var executor = CreateExecutor(new RecordingReporter(), transport, new RateLimitSettings { MaxRetries = 5 });

        var result = await executor.ExecuteAsync(new ApiRequest(HttpMethod.Get, "/items", AuthProfile: "anonymous"));

        Assert.Equal(HttpStatusCode.BadRequest, result.Response.StatusCode);
        Assert.Equal(1, result.Attempts);
        Assert.Empty(result.RetryEvents);
        Assert.Equal(1, transport.Calls);
    }

    [Fact(DisplayName = "Executor stops retrying when cancellation arrives during the retry delay")]
    public async Task ExecutorHonoursCancellationBeforeSecondAttempt()
    {
        using var cancellation = new CancellationTokenSource();
        var transport = new CancelAfterFirstResponseTransport(cancellation, Response(
            HttpStatusCode.TooManyRequests,
            "{\"error\":\"busy\"}",
            new Dictionary<string, string> { ["Retry-After"] = "1" }));
        var executor = CreateExecutor(new RecordingReporter(), transport, new RateLimitSettings
        {
            MaxRetries = 3,
            DefaultDelaySeconds = 1,
            MaxDelaySeconds = 1,
            RetryStatuses = [429],
            DelayHeaders = ["Retry-After"]
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteAsync(
            new ApiRequest(HttpMethod.Get, "/items", AuthProfile: "anonymous"),
            cancellation.Token));

        Assert.Equal(1, transport.Calls);
    }

    [Fact(DisplayName = "Executor keeps a shared HttpClient timeout unchanged")]
    public void ExecutorDoesNotMutateSharedHttpClientTimeout()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        _ = new ApiExecutor(
            client,
            new ApiSettings
            {
                BaseUrl = "https://example.test",
                TimeoutSeconds = 5,
                DefaultAuthProfile = "anonymous",
                AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["anonymous"] = new AuthProfileSettings { Type = "none" }
                }
            },
            new RecordingReporter(),
            new ConfiguredApiAuthenticator(new ApiSettings
            {
                AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["anonymous"] = new AuthProfileSettings { Type = "none" }
                }
            }),
            new SequenceTransport(Response(HttpStatusCode.OK, "{}")),
            new ApiUrlBuilder(new ApiSettings { BaseUrl = "https://example.test" }));

        Assert.Equal(TimeSpan.FromMinutes(2), client.Timeout);
    }

    [Fact(DisplayName = "Executor preserves the original transport timeout exception")]
    public async Task ExecutorPreservesOriginalTimeoutException()
    {
        var expected = new TimeoutException("transport timeout");
        var executor = CreateExecutor(new RecordingReporter(), new ThrowingTransport(expected), new RateLimitSettings());

        var actual = await Assert.ThrowsAsync<TimeoutException>(() => executor.ExecuteAsync(
            new ApiRequest(HttpMethod.Get, "/items", AuthProfile: "anonymous")));

        Assert.Same(expected, actual);
    }

    [Fact(DisplayName = "Executor preserves request JSON evidence and route metadata")]
    public async Task ExecutorReportsRequestAndRouteEvidence()
    {
        var reporter = new RecordingReporter();
        var route = new ApiRoute(
            "items.create",
            HttpMethod.Post,
            "/items",
            "items",
            "create",
            ApiRouteAuthentication.Required(ApiAuthType.Bearer, "default"));
        var executor = CreateExecutor(reporter, new SequenceTransport(Response(HttpStatusCode.Created, "{\"id\":1}")), new RateLimitSettings());

        await executor.ExecuteAsync(route.CreateRequest(
            bodyJson: "{\"name\":\"demo\"}",
            values: []));

        Assert.Equal("items.create", reporter.Details["route.name"]);
        Assert.Equal("bearer", reporter.Details["route.auth.type"]);
        Assert.Equal("required", reporter.Details["route.auth.requirement"]);
        Assert.Equal("default", reporter.Details["route.auth.profile"]);
        Assert.Equal("bearer required", reporter.Details["route.auth.policy"]);
        Assert.Contains(reporter.JsonPayloads, item => item.Label == "request.body" && item.Json.Contains('\n'));
        Assert.Contains(reporter.JsonPayloads, item => item.Label == "response.body");
    }

    [Fact(DisplayName = "Configured authenticator describes a missing bearer token without leaking a value")]
    public void MissingBearerTokenIsSafeToReport()
    {
        var settings = new ApiSettings
        {
            TokenEnvironmentVariable = "ATF_TEST_TOKEN_MISSING",
            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["default"] = new AuthProfileSettings { Type = "bearer" }
            }
        };
        var authenticator = new ConfiguredApiAuthenticator(settings, _ => null);
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://example.test/items");

        authenticator.Apply(message, "default");

        Assert.Null(message.Headers.Authorization);
        Assert.Equal("missing (ATF_TEST_TOKEN_MISSING)", authenticator.DescribeTokenState("default"));
    }

    [Fact(DisplayName = "Unknown and unsupported authentication profiles fail explicitly")]
    public void AuthenticatorRejectsUnknownAndUnsupportedProfiles()
    {
        var settings = new ApiSettings
        {
            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["custom"] = new AuthProfileSettings { Type = "oauth2" }
            }
        };
        var authenticator = new ConfiguredApiAuthenticator(settings, _ => null);
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        Assert.Throws<InvalidOperationException>(() => authenticator.Apply(message, "missing"));
        var exception = Assert.Throws<InvalidOperationException>(() => authenticator.Apply(message, "custom"));
        Assert.Contains("Unsupported auth profile", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Authenticator applies the bearer strategy")]
    public void AuthenticatorAppliesBearerStrategy()
    {
        var authenticator = new ConfiguredApiAuthenticator(SettingsWith("bearer", tokenVariable: "ATF_BEARER"),
            name => name == "ATF_BEARER" ? "secret-token" : null);
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        authenticator.Apply(message, "default");

        Assert.Equal("Bearer", message.Headers.Authorization?.Scheme);
        Assert.Equal("secret-token", message.Headers.Authorization?.Parameter);
        Assert.Equal("bearer token available", authenticator.DescribeTokenState("default"));
    }

    [Fact(DisplayName = "Authenticator applies the API key strategy")]
    public void AuthenticatorAppliesApiKeyStrategy()
    {
        var settings = SettingsWith("api-key", tokenVariable: "ATF_API_KEY", headerName: "X-Test-Key");
        var authenticator = new ConfiguredApiAuthenticator(settings,
            name => name == "ATF_API_KEY" ? "secret-key" : null);
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        authenticator.Apply(message, "default");

        Assert.Equal("secret-key", message.Headers.GetValues("X-Test-Key").Single());
        Assert.Equal("api key available", authenticator.DescribeTokenState("default"));
    }

    [Fact(DisplayName = "Authenticator applies the basic strategy")]
    public void AuthenticatorAppliesBasicStrategy()
    {
        var settings = SettingsWith("basic", usernameVariable: "ATF_USER", passwordVariable: "ATF_PASSWORD");
        var authenticator = new ConfiguredApiAuthenticator(settings,
            name => name switch
            {
                "ATF_USER" => "alice",
                "ATF_PASSWORD" => "p@ss",
                _ => null
            });
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        authenticator.Apply(message, "default");

        Assert.Equal("Basic", message.Headers.Authorization?.Scheme);
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("alice:p@ss")), message.Headers.Authorization?.Parameter);
        Assert.Equal("basic credentials available", authenticator.DescribeTokenState("default"));
    }

    [Fact(DisplayName = "Authenticator applies configured custom headers")]
    public void AuthenticatorAppliesCustomHeadersStrategy()
    {
        var settings = SettingsWith("custom", headers: new Dictionary<string, string?>
        {
            ["X-Correlation"] = "fixed",
            ["X-Trace"] = "${ATF_TRACE}"
        });
        var authenticator = new ConfiguredApiAuthenticator(settings,
            name => name == "ATF_TRACE" ? "trace-value" : null);
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        authenticator.Apply(message, "default");

        Assert.Equal("fixed", message.Headers.GetValues("X-Correlation").Single());
        Assert.Equal("trace-value", message.Headers.GetValues("X-Trace").Single());
        Assert.Equal("custom headers configured", authenticator.DescribeTokenState("default"));
    }

    [Fact(DisplayName = "Executor enforces its own timeout without mutating the shared client")]
    public async Task ExecutorCancelsTransportWhenExecutionBudgetExpires()
    {
        var transport = new BlockingTransport();
        var executor = CreateExecutor(new RecordingReporter(), transport, new RateLimitSettings(), timeoutSeconds: 1);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteAsync(
            new ApiRequest(HttpMethod.Get, "/slow", AuthProfile: "anonymous")));

        Assert.Equal(1, transport.Calls);
        Assert.True(transport.CancellationObserved);
    }

    [Fact(DisplayName = "Executor propagates external cancellation before transport work")]
    public async Task ExecutorHonoursExternalCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var transport = new BlockingTransport();
        var executor = CreateExecutor(new RecordingReporter(), transport, new RateLimitSettings(), timeoutSeconds: 5);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteAsync(
            new ApiRequest(HttpMethod.Get, "/cancelled", AuthProfile: "anonymous"), cancellation.Token));

        Assert.Equal(1, transport.Calls);
        Assert.True(transport.CancellationObserved);
    }

    private static ApiExecutor CreateExecutor(
        IAtfReporter reporter,
        IApiTransport transport,
        RateLimitSettings rateLimit,
        int timeoutSeconds = 5) =>
        new(
            new HttpClient(),
            new ApiSettings
            {
                BaseUrl = "https://example.test",
                TimeoutSeconds = timeoutSeconds,
                DefaultAuthProfile = "anonymous",
                AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["anonymous"] = new AuthProfileSettings { Type = "none" },
                    ["default"] = new AuthProfileSettings { Type = "none" }
                },
                RateLimit = rateLimit
            },
            reporter,
            new ConfiguredApiAuthenticator(new ApiSettings
            {
                AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["anonymous"] = new AuthProfileSettings { Type = "none" },
                    ["default"] = new AuthProfileSettings { Type = "none" }
                }
            }),
            transport,
            new ApiUrlBuilder(new ApiSettings { BaseUrl = "https://example.test" }));

    private static ApiSettings SettingsWith(
        string type,
        string? tokenVariable = null,
        string? headerName = null,
        string? usernameVariable = null,
        string? passwordVariable = null,
        Dictionary<string, string?>? headers = null) =>
        new()
        {
            TokenEnvironmentVariable = tokenVariable ?? "ATF_TOKEN",
            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["default"] = new AuthProfileSettings
                {
                    Type = type,
                    HeaderName = headerName,
                    UsernameEnvironmentVariable = usernameVariable,
                    PasswordEnvironmentVariable = passwordVariable,
                    Headers = headers ?? new Dictionary<string, string?>()
                }
            }
        };

    private static ApiResponse Response(
        HttpStatusCode status,
        string body,
        IReadOnlyDictionary<string, string>? headers = null) =>
        new(status, body, headers ?? new Dictionary<string, string>(), TimeSpan.Zero, "https://example.test");

    private sealed class SequenceTransport(params ApiResponse[] responses) : IApiTransport
    {
        private readonly Queue<ApiResponse> _responses = new(responses);
        public int Calls { get; private set; }

        public Task<ApiResponse> SendAsync(ApiRequest request, string url, string authProfile, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult((_responses.Count > 1 ? _responses.Dequeue() : _responses.Peek()) with { Url = url });
        }
    }

    private sealed class CancelAfterFirstResponseTransport(
        CancellationTokenSource cancellation,
        ApiResponse response) : IApiTransport
    {
        public int Calls { get; private set; }

        public Task<ApiResponse> SendAsync(
            ApiRequest request,
            string url,
            string authProfile,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            cancellation.Cancel();
            return Task.FromResult(response with { Url = url });
        }
    }

    private sealed class ThrowingTransport(Exception exception) : IApiTransport
    {
        public Task<ApiResponse> SendAsync(
            ApiRequest request,
            string url,
            string authProfile,
            CancellationToken cancellationToken = default) =>
            Task.FromException<ApiResponse>(exception);
    }

    private sealed class BlockingTransport : IApiTransport
    {
        public int Calls { get; private set; }
        public bool CancellationObserved { get; private set; }

        public async Task<ApiResponse> SendAsync(
            ApiRequest request,
            string url,
            string authProfile,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                CancellationObserved = true;
                throw;
            }

            throw new InvalidOperationException("Blocking transport completed unexpectedly.");
        }
    }

    private sealed class RecordingReporter : IAtfReporter
    {
        public Dictionary<string, string> Details { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<(string Label, string Json)> JsonPayloads { get; } = [];
        public List<string> Infos { get; } = [];

        public void StartStep(string title) { }
        public void CompleteStep(AtfReportStatus status) { }
        public void Detail(string key, string value) => Details[key] = value;
        public void Json(string label, string json) => JsonPayloads.Add((label, json));
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) { }
        public void Info(string message) => Infos.Add(message);
        public void Pass(string message) { }
        public void Fail(string message, Exception? exception = null) { }
        public void Warning(string message) { }
        public void Skip(string reason) { }
    }
}
