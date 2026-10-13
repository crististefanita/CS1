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
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApiConfigurationValidator.Validate(new ApiSettings { BaseUrl = "/relative" }));

        Assert.Contains("absolute URI", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Route configuration rule owns duplicate route diagnostics")]
    public void RouteRuleRejectsDuplicatePaths()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RouteConfigurationValidator.Validate(new RouteSettings
            {
                Users = "/same",
                Posts = "/same",
                UserPosts = "/different"
            }));

        Assert.Contains("duplicate route paths", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Reporting configuration rule owns timestamp diagnostics")]
    public void ReportingRuleRejectsInvalidTimestampFormat()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
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

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AuthProfileConfigurationValidator.Validate(profiles));

        Assert.Contains("PasswordEnvironmentVariable", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Redaction configuration rule owns local-path replacement diagnostics")]
    public void RedactionRuleRejectsMissingLocalPathReplacement()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RedactionConfigurationValidator.Validate(new RedactionSettings
            {
                RedactLocalPaths = true,
                LocalPathReplacement = string.Empty
            }));

        Assert.Contains("LocalPathReplacement", exception.Message, StringComparison.Ordinal);
    }
}
