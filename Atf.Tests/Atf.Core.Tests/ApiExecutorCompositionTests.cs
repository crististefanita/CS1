using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;

namespace Atf.Core.Tests;

public sealed class ApiExecutorCompositionTests
{
    [Fact(DisplayName = "API executor orchestrates injected URL, auth, transport and reporter contracts")]
    public async Task ExecutorUsesInjectedCollaborators()
    {
        var reporter = new RecordingReporter();
        var authenticator = new FakeAuthenticator();
        var transport = new FakeTransport(new ApiResponse(
            HttpStatusCode.OK,
            "{\"ok\":true}",
            new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            TimeSpan.FromMilliseconds(12),
            "https://example.test/custom"));
        var urlBuilder = new FakeUrlBuilder("https://example.test/custom");
        var executor = new ApiExecutor(
            new HttpClient(),
            new ApiSettings
            {
                BaseUrl = "https://example.test",
                TimeoutSeconds = 5,
                DefaultAuthProfile = "anonymous",
                AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["anonymous"] = new AuthProfileSettings { Type = "none" }
                },
                RateLimit = new RateLimitSettings { MaxRetries = 0, RetryStatuses = [429] }
            },
            reporter,
            authenticator,
            transport,
            urlBuilder);

        var result = await executor.ExecuteAsync(new ApiRequest(
            HttpMethod.Get,
            "/custom",
            AuthProfile: "anonymous"));

        Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
        Assert.Equal(1, transport.Calls);
        Assert.Equal("https://example.test/custom", urlBuilder.LastRequestUrl);
        Assert.Equal("anonymous", transport.LastAuthProfile);
        Assert.Equal("GET /custom", reporter.StartedStep);
        Assert.Contains("HTTP exchange completed; contract validation pending.", reporter.Infos);
    }

    [Fact(DisplayName = "API executor preserves the original exception when reporter failure also occurs")]
    public async Task ExecutorPreservesTransportException()
    {
        var expected = new HttpRequestException("network unavailable");
        var reporter = new RecordingReporter { ThrowWhenFailing = true };
        var executor = new ApiExecutor(
            new HttpClient(),
            new ApiSettings
            {
                BaseUrl = "https://example.test",
                TimeoutSeconds = 5,
                DefaultAuthProfile = "anonymous",
                AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["anonymous"] = new AuthProfileSettings { Type = "none" }
                },
                RateLimit = new RateLimitSettings { MaxRetries = 0 }
            },
            reporter,
            new FakeAuthenticator(),
            new ThrowingTransport(expected),
            new FakeUrlBuilder("https://example.test/items"));

        var actual = await Assert.ThrowsAsync<HttpRequestException>(() =>
            executor.ExecuteAsync(new ApiRequest(HttpMethod.Get, "/items", AuthProfile: "anonymous")));

        Assert.Same(expected, actual);
    }

    private sealed class FakeUrlBuilder(string url) : IApiUrlBuilder
    {
        public string? LastRequestUrl { get; private set; }

        public string Build(ApiRequest request)
        {
            LastRequestUrl = url;
            return url;
        }
    }

    private sealed class FakeAuthenticator : IApiAuthenticator
    {
        public string? LastProfile { get; private set; }

        public void Apply(HttpRequestMessage message, string profileName) => LastProfile = profileName;
        public string DescribeMode(string profileName) => "fake";
        public string DescribeTokenState(string profileName) => "not applicable";
    }

    private sealed class FakeTransport(ApiResponse response) : IApiTransport
    {
        public int Calls { get; private set; }
        public string? LastAuthProfile { get; private set; }

        public Task<ApiResponse> SendAsync(ApiRequest request, string url, string authProfile, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastAuthProfile = authProfile;
            return Task.FromResult(response with { Url = url });
        }
    }

    private sealed class ThrowingTransport(Exception exception) : IApiTransport
    {
        public Task<ApiResponse> SendAsync(ApiRequest request, string url, string authProfile, CancellationToken cancellationToken = default) =>
            Task.FromException<ApiResponse>(exception);
    }

    private sealed class RecordingReporter : IAtfReporter
    {
        public string? StartedStep { get; private set; }
        public bool ThrowWhenFailing { get; init; }
        public List<string> Infos { get; } = [];

        public void StartStep(string title) => StartedStep = title;
        public void Detail(string key, string value) { }
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) { }
        public void Info(string message) => Infos.Add(message);
        public void Pass(string message) { }
        public void Fail(string message, Exception? exception = null)
        {
            if (ThrowWhenFailing)
            {
                throw new InvalidOperationException("reporting failed");
            }
        }
        public void Warning(string message) { }
        public void Skip(string reason) { }
    }
}
