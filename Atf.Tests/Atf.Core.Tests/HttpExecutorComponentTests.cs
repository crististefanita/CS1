using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;

namespace Atf.Core.Tests;

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
        Assert.Contains("rate-limit.retry.reason", reporter.Details.Keys);
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
        Assert.Contains(reporter.JsonPayloads, item => item.Label == "request.body" && item.Json.Contains("\n", StringComparison.Ordinal));
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
                ["custom"] = new AuthProfileSettings { Type = "api-key" }
            }
        };
        var authenticator = new ConfiguredApiAuthenticator(settings, _ => null);
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        Assert.Throws<InvalidOperationException>(() => authenticator.Apply(message, "missing"));
        var exception = Assert.Throws<InvalidOperationException>(() => authenticator.Apply(message, "custom"));
        Assert.Contains("Unsupported auth profile", exception.Message, StringComparison.Ordinal);
    }

    private static ApiExecutor CreateExecutor(IAtfReporter reporter, IApiTransport transport, RateLimitSettings rateLimit) =>
        new(
            new HttpClient(),
            new ApiSettings
            {
                BaseUrl = "https://example.test",
                TimeoutSeconds = 5,
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

    private sealed class RecordingReporter : IAtfReporter
    {
        public Dictionary<string, string> Details { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<(string Label, string Json)> JsonPayloads { get; } = [];

        public void StartStep(string title) { }
        public void Detail(string key, string value) => Details[key] = value;
        public void Json(string label, string json) => JsonPayloads.Add((label, json));
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) { }
        public void Info(string message) { }
        public void Pass(string message) { }
        public void Fail(string message, Exception? exception = null) { }
        public void Warning(string message) { }
        public void Skip(string reason) { }
    }
}
