using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Core.Reporting;
using Atf.Core.Resilience;
using System.Net;
using System.Net.Http.Headers;

namespace Atf.Core.Tests.Architecture;

public sealed class CoreArchitectureTests
{
    [Fact(DisplayName = "ATF reporting contracts use runner-neutral status values")]
    public void ReportingContractsDoNotExposeExtentStatus()
    {
        var statusParameter = typeof(IAtfReporter)
            .GetMethod(nameof(IAtfReporter.CompleteStep))!
            .GetParameters()
            .Single()
            .ParameterType;

        Assert.Equal(typeof(AtfReportStatus), statusParameter);
        Assert.NotEqual("Status", statusParameter.FullName);
    }

    [Fact(DisplayName = "Report session exposes a renderer-neutral scenario scope")]
    public void ReportSessionUsesScenarioPort()
    {
        var returnType = typeof(IAtfReportSession)
            .GetMethod(nameof(IAtfReportSession.StartTest))!
            .ReturnType;

        Assert.Equal(typeof(IAtfScenarioScope), returnType);
        Assert.DoesNotContain("AventStack", returnType.AssemblyQualifiedName ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Report theme uses one central font family and central report colors")]
    public void ReportThemeUsesCentralTypographyAndPalette()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains($"--atf-font-sans: {AtfReportTypography.SansFontFamily};", css, StringComparison.Ordinal);
        Assert.Contains($"--atf-font-code: {AtfReportTypography.CodeFontFamily};", css, StringComparison.Ordinal);
        Assert.Contains($"--atf-font-base-size: {AtfReportTypography.BaseFontSize};", css, StringComparison.Ordinal);
        Assert.Contains($"--atf-overview-font-size: {AtfReportTypography.OverviewFontSize};", css, StringComparison.Ordinal);
        Assert.Contains($"--atf-json-font-size: {AtfReportTypography.JsonFontSize};", css, StringComparison.Ordinal);
        Assert.Contains($"--atf-border: {AtfReportColors.Border};", css, StringComparison.Ordinal);
        Assert.Contains($"--atf-reference-border: {AtfReportColors.ReferenceBorder};", css, StringComparison.Ordinal);
        Assert.Contains($"--atf-text: {AtfReportColors.Text};", css, StringComparison.Ordinal);
        Assert.DoesNotContain("font-family: Inter, ui-sans-serif", css, StringComparison.Ordinal);
        Assert.DoesNotContain("font-family: 'Segoe UI', 'Inter', 'Helvetica Neue', Arial, sans-serif !important;", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Report theme keeps the reference overview layout and nested dark surface")]
    public void ReportThemeKeepsReferenceOverviewLayout()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains("grid-template-columns: repeat(4, minmax(0, 1fr))", css, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 1100px)", css, StringComparison.Ordinal);
        Assert.Contains(".atf-tag-card .card-header", css, StringComparison.Ordinal);
        Assert.Contains("background: var(--atf-panel-2) !important", css, StringComparison.Ordinal);
        Assert.Contains("background: rgba(255,255,255,.018) !important", css, StringComparison.Ordinal);
        Assert.Contains(".dashboard-view .card > .card-body h3", css, StringComparison.Ordinal);
        Assert.Contains(".dashboard-view .card > .card-header p", css, StringComparison.Ordinal);
        Assert.Contains(".dashboard-view .row > .col-md-3 > .card", css, StringComparison.Ordinal);
        Assert.Contains("border-color: var(--atf-reference-border) !important", css, StringComparison.Ordinal);
        Assert.Contains("background: var(--atf-panel) !important", css, StringComparison.Ordinal);
        Assert.Contains("border-color: var(--atf-border) !important", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".test-view .test-content-detail .card[data-atf-phase] .accordion .node", css, StringComparison.Ordinal);
        Assert.Contains(".test-view .test-content-detail .card > .card-header .card-title .node", css, StringComparison.Ordinal);
        Assert.Contains("color: var(--atf-text) !important", css, StringComparison.Ordinal);
        Assert.DoesNotContain("Extent's native nested API cards", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Configuration loader binds module-owned settings and validates the contract")]
    public void ConfigurationLoaderBindsModuleSettings()
    {
        var root = Directory.CreateTempSubdirectory("atf-config-");
        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "appsettings.json"), """
            {
              "Modules": {
                "Api": {
                  "Config": {
                    "Api": { "BaseUrl": "https://example.test" },
                    "Reporting": { "Engine": "extent" }
                  }
                }
              }
            }
            """);

            var configuration = new AtfConfigurationLoader(
                _ => null,
                new Dictionary<string, string?>()).Load(root.FullName);

            Assert.Equal("https://example.test", configuration.Api.BaseUrl);
            Assert.Equal("extent", configuration.Reporting.Engine);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact(DisplayName = "Configuration validation rejects a missing API module")]
    public void ConfigurationValidationRejectsMissingApiModule()
    {
        var configuration = new AtfConfiguration();

        var exception = Assert.Throws<InvalidOperationException>(configuration.Validate);

        Assert.Contains("Modules:Api", exception.Message);
    }

    [Fact(DisplayName = "URL builder encodes query values and preserves the configured base URL")]
    public void UrlBuilderEncodesQueryValues()
    {
        var builder = new ApiUrlBuilder(new ApiSettings { BaseUrl = "https://example.test/api/" });

        var url = builder.Build(new ApiRequest(
            HttpMethod.Get,
            "/users",
            Query: new Dictionary<string, string?> { ["filter"] = "a/b & c", ["ignored"] = null }));

        Assert.Equal("https://example.test/api/users?filter=a%2Fb%20%26%20c", url);
    }

    [Fact(DisplayName = "Bearer authenticator applies a token without exposing it in descriptions")]
    public void BearerAuthenticatorAppliesTokenSafely()
    {
        var settings = new ApiSettings
        {
            TokenEnvironmentVariable = "TOKEN",
            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["default"] = new AuthProfileSettings { Type = "bearer" }
            }
        };
        var authenticator = new ConfiguredApiAuthenticator(settings, name => name == "TOKEN" ? "secret-value" : null);
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        authenticator.Apply(message, "default");

        Assert.Equal("Bearer", message.Headers.Authorization?.Scheme);
        Assert.Equal("secret-value", message.Headers.Authorization?.Parameter);
        Assert.Equal("bearer", authenticator.DescribeMode("default"));
        Assert.Equal("bearer token available", authenticator.DescribeTokenState("default"));
    }

    [Fact(DisplayName = "HTTP transport carries JSON and configured authentication")]
    public async Task HttpTransportBuildsTheExpectedRequest()
    {
        HttpMethod? capturedMethod = null;
        AuthenticationHeaderValue? capturedAuthorization = null;
        string? capturedBody = null;
        using var client = new HttpClient(new RecordingHandler(request =>
        {
            capturedMethod = request.Method;
            capturedAuthorization = request.Headers.Authorization;
            capturedBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent("{\"ok\":true}", System.Text.Encoding.UTF8, "application/json")
            };
        }));
        var settings = new ApiSettings
        {
            TokenEnvironmentVariable = "TOKEN",
            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["default"] = new AuthProfileSettings { Type = "bearer" }
            }
        };
        var transport = new HttpClientTransport(
            client,
            new ConfiguredApiAuthenticator(settings, _ => "test-token"));

        var response = await transport.SendAsync(
            new ApiRequest(HttpMethod.Post, "/items", "{\"name\":\"demo\"}"),
            "https://example.test/items",
            "default");

        Assert.Equal(HttpMethod.Post, capturedMethod);
        Assert.Equal("Bearer test-token", capturedAuthorization?.ToString());
        Assert.Equal("{\"name\":\"demo\"}", capturedBody);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Header("Content-Type"));
    }

    [Fact(DisplayName = "Response headers are read case-insensitively even from ordinary dictionaries")]
    public void ResponseHeaderLookupIgnoresCase()
    {
        var response = new ApiResponse(
            HttpStatusCode.OK,
            "{}",
            new Dictionary<string, string> { ["content-type"] = "application/json" },
            TimeSpan.Zero,
            "https://example.test");

        Assert.Equal("application/json", response.Header("Content-Type"));
    }

    [Fact(DisplayName = "Retry policy honours budget, status and server delay")]
    public void RetryPolicyHonoursConfiguredBudget()
    {
        var policy = new RateLimitRetryPolicy(new RateLimitSettings
        {
            MaxRetries = 1,
            DefaultDelaySeconds = 5,
            MaxDelaySeconds = 10,
            RetryStatuses = [429],
            DelayHeaders = ["Retry-After"]
        });
        var response = new ApiResponse(
            (HttpStatusCode)429,
            "rate limited",
            new Dictionary<string, string> { ["Retry-After"] = "3" },
            TimeSpan.Zero,
            "https://example.test");

        var first = policy.Decide(response, attempt: 0);
        var exhausted = policy.Decide(response, attempt: 1);

        Assert.True(first.ShouldRetry);
        Assert.Equal(TimeSpan.FromSeconds(3), first.Delay);
        Assert.False(exhausted.ShouldRetry);
        Assert.Equal("Retry budget exhausted.", exhausted.Reason);
    }

    [Fact(DisplayName = "Polling stops at the first satisfied value")]
    public async Task PollingStopsAtSatisfiedValue()
    {
        var values = new Queue<int>([1, 2, 3]);
        var result = await Polling.UntilAsync(
            _ => Task.FromResult(values.Dequeue()),
            value => value == 2,
            timeout: TimeSpan.FromSeconds(1),
            interval: TimeSpan.Zero);

        Assert.True(result.Satisfied);
        Assert.Equal(2, result.LastValue);
        Assert.Equal(2, result.Attempts);
    }

    [Fact(DisplayName = "Report path resolver prefers an existing configured project root")]
    public void ReportPathResolverUsesConfiguredRoot()
    {
        var root = Directory.CreateTempSubdirectory("atf-report-");
        try
        {
            var resolver = new ReportPathResolver(
                _ => root.FullName,
                currentDirectory: root.FullName,
                baseDirectory: root.FullName);

            var path = resolver.Resolve("artifacts/reports/report.html");

            Assert.Equal(Path.GetFullPath(Path.Combine(root.FullName, "artifacts/reports/report.html")), path);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact(DisplayName = "Report asset cleaner removes only the sibling Extent asset directory")]
    public void ReportAssetCleanerRemovesExtentFolder()
    {
        var root = Directory.CreateTempSubdirectory("atf-assets-");
        try
        {
            var report = Path.Combine(root.FullName, "report.html");
            var assets = Directory.CreateDirectory(Path.Combine(root.FullName, "extent"));
            File.WriteAllText(report, "<html></html>");
            File.WriteAllText(Path.Combine(assets.FullName, "spark.css"), "body{}");

            new ReportAssetCleaner().Clean(report);

            Assert.False(Directory.Exists(assets.FullName));
            Assert.True(File.Exists(report));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(callback(request));
    }
}
