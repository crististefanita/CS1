using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Core.Reporting;
using System.Net;
using Xunit;

namespace Atf.Api;

public sealed class ApiExecutorReportingTests
{
    [Fact(DisplayName = "HTTP response completion is informational, not a functional pass")]
    public async Task NonRetryableServerResponseDoesNotEmitPass()
    {
        using var client = new HttpClient(new StubHandler(() => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("error code: 502")
        }));
        var reporter = new RecordingReporter();
        var settings = new ApiSettings
        {
            BaseUrl = "https://example.test",
            TimeoutSeconds = 5,
            DefaultAuthProfile = "anonymous",
            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["anonymous"] = new AuthProfileSettings { Type = "none" }
            },
            RateLimit = new RateLimitSettings
            {
                MaxRetries = 0,
                DefaultDelaySeconds = 1,
                MaxDelaySeconds = 1,
                RetryStatuses = [429]
            }
        };

        var result = await new ApiExecutor(client, settings, reporter)
            .ExecuteAsync(
                new ApiRequest(HttpMethod.Get, "/items", AuthProfile: "anonymous"),
                TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, result.Response.StatusCode);
        Assert.Contains("HTTP exchange completed; contract validation pending.", reporter.InfoMessages);
        Assert.Empty(reporter.PassMessages);
        Assert.Equal("HTTP GET", reporter.Details["request.type"]);
        Assert.Equal("anonymous", reporter.Details["auth.profile"]);
        Assert.Equal("none", reporter.Details["auth.mode"]);
        Assert.Equal("not applicable", reporter.Details["auth.token.state"]);
        Assert.Equal("502", reporter.Details["response.status"]);
        Assert.Contains("exchange.duration.ms", reporter.Details.Keys);
        Assert.Equal(["start:GET /items", "complete:Info"], reporter.StepEvents);
    }

    private sealed class StubHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory());
    }

    private sealed class RecordingReporter : IAtfReporter
    {
        public List<string> InfoMessages { get; } = [];
        public List<string> PassMessages { get; } = [];
        public List<string> StepEvents { get; } = [];
        public Dictionary<string, string> Details { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void StartStep(string title) => StepEvents.Add($"start:{title}");
        public void CompleteStep(AtfReportStatus status) => StepEvents.Add($"complete:{status}");
        public void Detail(string key, string value) => Details[key] = value;
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) { }
        public void Info(string message) => InfoMessages.Add(message);
        public void Pass(string message) => PassMessages.Add(message);
        public void Fail(string message, Exception? exception = null) => throw exception ?? new InvalidOperationException(message);
        public void Warning(string message) { }
        public void Skip(string reason) { }
    }
}
