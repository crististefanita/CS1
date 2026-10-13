using Atf.Core.Configuration;
using Atf.Core.Configuration.Models;
using Atf.Core.Configuration.Security;
using Atf.Core.Configuration.Validation;

namespace Atf.Core.Tests.Configuration;

public sealed class ConfigurationRuleTests
{
    [Fact(DisplayName = "API configuration rule owns base URL diagnostics")]
    public void ApiRuleRejectsRelativeBaseUrl()
    {
        var exception = Assert.Throws<AtfConfigurationException>(() =>
            ApiConfigurationValidator.Validate(new ApiSettings { BaseUrl = "/relative" }));

        Assert.Equal("ATF-API-002", exception.Code);
        Assert.Equal("Modules:Api:Api:BaseUrl", exception.Path);
        Assert.Contains("absolute URI", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Configuration diagnostics expose a stable code and path for invalid timing")]
    public void ApiRulePublishesTimingDiagnostic()
    {
        var exception = Assert.Throws<AtfConfigurationException>(() =>
            ApiConfigurationValidator.Validate(new ApiSettings
            {
                BaseUrl = "https://example.test",
                TimeoutSeconds = 0
            }));

        Assert.Equal("ATF-API-003", exception.Code);
        Assert.Equal("Modules:Api:Api:TimeoutSeconds", exception.Path);
        Assert.Contains("positive", exception.Detail, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Route configuration rule owns duplicate route diagnostics")]
    public void RouteRuleRejectsDuplicatePaths()
    {
        var exception = Assert.Throws<AtfConfigurationException>(() =>
            RouteConfigurationValidator.Validate(new RouteSettings
            {
                Users = "/same",
                Posts = "/same",
                UserPosts = "/different"
            }));

        Assert.Equal("ATF-ROUTE-002", exception.Code);
        Assert.Equal("Modules:Api:Api:Routes", exception.Path);
        Assert.Contains("duplicate route paths", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "API rule rejects an unknown default authentication profile")]
    public void ApiRuleRejectsUnknownDefaultAuthProfile()
    {
        var exception = Assert.Throws<AtfConfigurationException>(() =>
            ApiConfigurationValidator.Validate(new ApiSettings
            {
                BaseUrl = "https://example.test",
                DefaultAuthProfile = "missing"
            }));

        Assert.Equal("ATF-AUTH-001", exception.Code);
        Assert.Equal("Modules:Api:Api:DefaultAuthProfile", exception.Path);
    }

    [Fact(DisplayName = "Reporting configuration rule owns timestamp diagnostics")]
    public void ReportingRuleRejectsInvalidTimestampFormat()
    {
        var exception = Assert.Throws<AtfConfigurationException>(() =>
            ReportingConfigurationValidator.Validate(new ReportingSettings { TimestampFormat = "[" }));

        Assert.Contains("TimestampFormat", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Authentication configuration rule owns profile-specific diagnostics")]
    public void AuthRuleRejectsIncompleteBasicProfile()
    {
        var profiles = new Dictionary<string, AuthProfileSettings>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = new() { Type = "basic", UsernameEnvironmentVariable = "ATF_USER" }
        };

        var exception = Assert.Throws<AtfConfigurationException>(() =>
            AuthProfileConfigurationValidator.Validate(profiles));

        Assert.Equal("ATF-AUTH-003", exception.Code);
        Assert.Equal("Modules:Api:Api:AuthProfiles:default", exception.Path);
        Assert.Contains("PasswordEnvironmentVariable", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Authentication rule rejects unsupported profile types with a stable path")]
    public void AuthRuleRejectsUnknownType()
    {
        var exception = Assert.Throws<AtfConfigurationException>(() =>
            AuthProfileConfigurationValidator.Validate(new Dictionary<string, AuthProfileSettings>
            {
                ["default"] = new() { Type = "mystery" }
            }));

        Assert.Equal("ATF-AUTH-002", exception.Code);
        Assert.Equal("Modules:Api:Api:AuthProfiles:default:Type", exception.Path);
    }

    [Fact(DisplayName = "Redaction configuration rule owns local-path replacement diagnostics")]
    public void RedactionRuleRejectsMissingLocalPathReplacement()
    {
        var exception = Assert.Throws<AtfConfigurationException>(() =>
            RedactionConfigurationValidator.Validate(new RedactionSettings
            {
                RedactLocalPaths = true,
                LocalPathReplacement = string.Empty
            }));

        Assert.Contains("LocalPathReplacement", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "API snapshot isolates nested dictionaries and route values")]
    public void ApiSnapshotDoesNotObserveLaterConfigurationMutation()
    {
        var authProfiles = new Dictionary<string, AuthProfileSettings>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = new() { Type = "none" }
        };
        var routes = new RouteSettings { Users = "/users" };
        var source = new ApiSettings
        {
            BaseUrl = "https://example.test",
            AuthProfiles = authProfiles,
            Routes = routes
        };

        var snapshot = source.Snapshot();
        authProfiles["default"].Headers["X-Test"] = "mutated";
        authProfiles["later"] = new AuthProfileSettings { Type = "bearer" };

        Assert.DoesNotContain("later", snapshot.AuthProfiles.Keys);
        Assert.DoesNotContain("X-Test", snapshot.AuthProfiles["default"].Headers.Keys);
        Assert.Equal("/users", snapshot.Routes.Users);
    }
}
