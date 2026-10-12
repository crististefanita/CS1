using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Core.Reporting;
using Atf.Core.Resilience;
using System.Net;

namespace Atf.Core.Tests.Configuration;

public sealed class ConfigurationAndResilienceTests
{
    [Fact(DisplayName = "Configuration rejects a non-absolute API base URL")]
    public void ConfigurationRejectsRelativeBaseUrl()
    {
        var configuration = new AtfConfiguration
        {
            Modules = new(StringComparer.OrdinalIgnoreCase)
            {
                ["api"] = new AtfModuleSettings
                {
                    Config = new AtfModuleConfig
                    {
                        Api = new ApiSettings { BaseUrl = "/api" },
                        Reporting = new ReportingSettings { Engine = "extent" }
                    }
                }
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(configuration.Validate);

        Assert.Contains("absolute URI", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Configuration rejects invalid timeout and polling values")]
    public void ConfigurationRejectsNonPositiveTiming()
    {
        var configuration = ValidConfiguration(new ApiSettings
        {
            BaseUrl = "https://example.test",
            TimeoutSeconds = 0,
            Polling = new PollingSettings { TimeoutSeconds = 1, IntervalMilliseconds = 1 }
        });

        var exception = Assert.Throws<InvalidOperationException>(configuration.Validate);

        Assert.Contains("timeouts and polling interval", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Configuration rejects a reporting engine other than Extent")]
    public void ConfigurationRejectsNonExtentReporting()
    {
        var configuration = ValidConfiguration(new ApiSettings { BaseUrl = "https://example.test" }, "allure");

        var exception = Assert.Throws<InvalidOperationException>(configuration.Validate);

        Assert.Contains("Extent-only", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Configuration rejects a non-positive assertion display limit")]
    public void ConfigurationRejectsInvalidAssertionDisplayLimit()
    {
        var configuration = new AtfConfiguration
        {
            Modules = new(StringComparer.OrdinalIgnoreCase)
            {
                ["api"] = new AtfModuleSettings
                {
                    Config = new AtfModuleConfig
                    {
                        Api = new ApiSettings
                        {
                            BaseUrl = "https://example.test",
                            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
                            {
                                ["default"] = new AuthProfileSettings { Type = "none" }
                            }
                        },
                        Reporting = new ReportingSettings
                        {
                            Engine = "extent",
                            AssertionMaxDisplayCharacters = 0
                        }
                    }
                }
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(configuration.Validate);

        Assert.Contains("AssertionMaxDisplayCharacters", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Configuration rejects an unconfigured default auth profile")]
    public void ConfigurationRejectsMissingDefaultAuthProfile()
    {
        var configuration = new AtfConfiguration
        {
            Modules = new(StringComparer.OrdinalIgnoreCase)
            {
                ["api"] = new AtfModuleSettings
                {
                    Config = new AtfModuleConfig
                    {
                        Api = new ApiSettings { BaseUrl = "https://example.test" },
                        Reporting = new ReportingSettings { Engine = "extent" }
                    }
                }
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(configuration.Validate);

        Assert.Contains("DefaultAuthProfile", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Configuration rejects local-path redaction without a replacement")]
    public void ConfigurationRejectsInvalidLocalPathReplacement()
    {
        var configuration = ValidConfiguration(new ApiSettings
        {
            BaseUrl = "https://example.test",
            Redaction = new Atf.Core.Configuration.Security.RedactionSettings
            {
                RedactLocalPaths = true,
                LocalPathReplacement = " "
            }
        });

        var exception = Assert.Throws<InvalidOperationException>(configuration.Validate);

        Assert.Contains("LocalPathReplacement", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Configuration rejects retry budgets whose maximum is below the default")]
    public void ConfigurationRejectsInvalidRetryIntervals()
    {
        var configuration = ValidConfiguration(new ApiSettings
        {
            BaseUrl = "https://example.test",
            RateLimit = new RateLimitSettings { DefaultDelaySeconds = 5, MaxDelaySeconds = 2 }
        });

        var exception = Assert.Throws<InvalidOperationException>(configuration.Validate);

        Assert.Contains("MaxDelaySeconds", exception.Message, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "Configuration rejects invalid report file contracts")]
    [InlineData("artifacts/report.json", "valid")]
    [InlineData("artifacts/report.html", "'bad")]
    public void ConfigurationRejectsInvalidReportPathOrTimestamp(string reportPath, string timestamp)
    {
        var configuration = new AtfConfiguration
        {
            Modules = new(StringComparer.OrdinalIgnoreCase)
            {
                ["api"] = new AtfModuleSettings
                {
                    Config = new AtfModuleConfig
                    {
                        Api = new ApiSettings
                        {
                            BaseUrl = "https://example.test",
                            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
                            {
                                ["default"] = new AuthProfileSettings { Type = "none" }
                            }
                        },
                        Reporting = new ReportingSettings
                        {
                            Engine = "extent",
                            ReportPath = reportPath,
                            TimestampFormat = timestamp
                        }
                    }
                }
            }
        };

        Assert.Throws<InvalidOperationException>(configuration.Validate);
    }

    [Fact(DisplayName = "Configuration validates authentication-specific required properties")]
    public void ConfigurationValidatesAuthProfileProperties()
    {
        var basic = ValidConfiguration(new ApiSettings
        {
            BaseUrl = "https://example.test",
            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["default"] = new AuthProfileSettings { Type = "basic" }
            }
        });

        var basicException = Assert.Throws<InvalidOperationException>(basic.Validate);
        Assert.Contains("UsernameEnvironmentVariable", basicException.Message, StringComparison.Ordinal);

        var apiKey = ValidConfiguration(new ApiSettings
        {
            BaseUrl = "https://example.test",
            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["default"] = new AuthProfileSettings { Type = "apikey" }
            }
        });

        var apiKeyException = Assert.Throws<InvalidOperationException>(apiKey.Validate);
        Assert.Contains("HeaderName", apiKeyException.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Configuration loader applies ATF environment overrides")]
    public void ConfigurationLoaderAppliesEnvironmentOverride()
    {
        var root = Directory.CreateTempSubdirectory("atf-config-env-");
        const string variable = "ATF_Modules__Api__Config__Api__BaseUrl";
        var previous = Environment.GetEnvironmentVariable(variable);
        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "appsettings.json"), """
            {
              "Modules": {
                "Api": {
                  "Config": {
                    "Api": {
                      "BaseUrl": "https://file.example.test",
                      "AuthProfiles": { "default": { "Type": "none" } }
                    },
                    "Reporting": { "Engine": "extent" }
                  }
                }
              }
            }
            """);
            var configuration = new AtfConfigurationLoader(
                _ => null,
                new Dictionary<string, string?>
                {
                    ["Modules:Api:Config:Api:BaseUrl"] = "https://environment.example.test",
                    ["Modules:Api:Config:Reporting:AssertionMaxDisplayCharacters"] = "96"
                }).Load(root.FullName);

            Assert.Equal("https://environment.example.test", configuration.Api.BaseUrl);
            Assert.Equal(96, configuration.Reporting.AssertionMaxDisplayCharacters);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous);
            root.Delete(recursive: true);
        }
    }

    [Fact(DisplayName = "Configuration loads deterministic report navigation order")]
    public void ConfigurationLoadsReportNavigationOrder()
    {
        var configuration = new AtfConfigurationLoader().Load();

        Assert.Equal(
            ["UsersApiTests", "PostsApiTests", "ApiLifecycleAndCombinationsTests"],
            configuration.Reporting.TestClassOrder);
        Assert.Contains(
            "Users API — setup → body → teardown",
            configuration.Reporting.TestScenarioOrder);
        Assert.Equal("api", configuration.Reporting.TagOrder[0]);
    }

    [Theory(DisplayName = "Retry policy uses fallback for unusable Retry-After values")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("0")]
    [InlineData("-4")]
    public void RetryPolicyUsesFallbackForInvalidHeader(string? header)
    {
        var policy = new RateLimitRetryPolicy(new RateLimitSettings
        {
            MaxRetries = 1,
            DefaultDelaySeconds = 4,
            MaxDelaySeconds = 10,
            RetryStatuses = [429],
            DelayHeaders = ["Retry-After"]
        });
        var headers = header is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["Retry-After"] = header };

        var decision = policy.Decide(Response(HttpStatusCode.TooManyRequests, headers), 0);

        Assert.True(decision.ShouldRetry);
        Assert.Equal(TimeSpan.FromSeconds(4), decision.Delay);
        Assert.Equal("Configured fallback delay.", decision.Reason);
    }

    [Fact(DisplayName = "Retry policy caps an excessive server delay")]
    public void RetryPolicyCapsServerDelay()
    {
        var policy = new RateLimitRetryPolicy(new RateLimitSettings
        {
            MaxRetries = 2,
            MaxDelaySeconds = 7,
            RetryStatuses = [429],
            DelayHeaders = ["Retry-After"]
        });

        var decision = policy.Decide(Response(HttpStatusCode.TooManyRequests,
            new Dictionary<string, string> { ["Retry-After"] = "99" }), 0);

        Assert.Equal(TimeSpan.FromSeconds(7), decision.Delay);
        Assert.Equal("Server retry header delay.", decision.Reason);
        Assert.Equal("99", decision.Header);
    }

    [Theory(DisplayName = "Retry policy completes immediately for non-retryable statuses")]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public void RetryPolicyDoesNotRetryOtherStatuses(HttpStatusCode status)
    {
        var policy = new RateLimitRetryPolicy(new RateLimitSettings
        {
            MaxRetries = 10,
            RetryStatuses = [429]
        });

        var decision = policy.Decide(Response(status), 0);

        Assert.False(decision.ShouldRetry);
        Assert.Equal(TimeSpan.Zero, decision.Delay);
        Assert.Equal("HTTP status is not retryable.", decision.Reason);
    }

    [Fact(DisplayName = "Polling returns the last observed value when the deadline expires")]
    public async Task PollingReturnsLastValueOnTimeout()
    {
        var calls = 0;
        var result = await Polling.UntilAsync(
            _ => Task.FromResult(++calls),
            value => value == 999,
            timeout: TimeSpan.Zero,
            interval: TimeSpan.Zero);

        Assert.False(result.Satisfied);
        Assert.Equal(1, result.Attempts);
        Assert.Equal(1, result.LastValue);
    }

    [Fact(DisplayName = "Polling observes cancellation before asking the supplier")]
    public async Task PollingHonoursCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var called = false;

        await Assert.ThrowsAsync<OperationCanceledException>(() => Polling.UntilAsync(
            _ =>
            {
                called = true;
                return Task.FromResult(true);
            },
            _ => true,
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero,
            source.Token));

        Assert.False(called);
    }

    [Fact(DisplayName = "Polling rejects negative timeout and interval before executing the supplier")]
    public async Task PollingRejectsNegativeTiming()
    {
        var supplierCalled = false;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Polling.UntilAsync(
            _ =>
            {
                supplierCalled = true;
                return Task.FromResult(true);
            },
            _ => true,
            TimeSpan.FromMilliseconds(-1),
            TimeSpan.Zero));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Polling.UntilAsync(
            _ => Task.FromResult(true),
            _ => true,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(-1)));

        Assert.False(supplierCalled);
    }

    [Fact(DisplayName = "JSON formatter prettifies valid JSON and preserves plain text")]
    public void JsonFormatterHandlesValidAndInvalidPayloads()
    {
        Assert.Contains("\n", JsonFormatter.Pretty("{\"id\":1,\"name\":\"demo\"}"), StringComparison.Ordinal);
        Assert.Equal("not-json", JsonFormatter.Pretty("not-json"));
        Assert.Equal("<empty>", JsonFormatter.Pretty("  "));
    }

    private static AtfConfiguration ValidConfiguration(ApiSettings api, string engine = "extent")
    {
        if (!api.AuthProfiles.ContainsKey(api.DefaultAuthProfile))
        {
            api.AuthProfiles[api.DefaultAuthProfile] = new AuthProfileSettings { Type = "none" };
        }

        return new AtfConfiguration
        {
            Modules = new(StringComparer.OrdinalIgnoreCase)
            {
                ["api"] = new AtfModuleSettings
                {
                    Config = new AtfModuleConfig { Api = api, Reporting = new ReportingSettings { Engine = engine } }
                }
            }
        };
    }

    private static ApiResponse Response(HttpStatusCode status, IReadOnlyDictionary<string, string>? headers = null) =>
        new(status, "{}", headers ?? new Dictionary<string, string>(), TimeSpan.Zero, "https://example.test");
}
