using Atf.Core.Configuration;
using System.Net.Http.Headers;
using System.Text;

namespace Atf.Core.Http.Transport.Authentication;

/// <summary>
/// Typed authentication behaviour behind the string-based configuration boundary.
/// Configuration stays friendly to JSON, while each supported mode owns its
/// header and diagnostic rules in one small strategy.
/// </summary>
internal interface IApiAuthenticationStrategy
{
    string Name { get; }

    void Apply(
        HttpRequestMessage message,
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader);

    string DescribeTokenState(
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader);
}

internal static class ApiAuthenticationStrategyCatalog
{
    private static readonly Dictionary<string, IApiAuthenticationStrategy> Strategies =
        new Dictionary<string, IApiAuthenticationStrategy>(StringComparer.OrdinalIgnoreCase)
        {
            ["none"] = new NoAuthenticationStrategy(),
            ["bearer"] = new BearerAuthenticationStrategy(),
            ["apikey"] = new ApiKeyAuthenticationStrategy(),
            ["api-key"] = new ApiKeyAuthenticationStrategy(),
            ["basic"] = new BasicAuthenticationStrategy(),
            ["custom"] = new CustomHeadersAuthenticationStrategy()
        };

    public static IApiAuthenticationStrategy Resolve(AuthProfileSettings profile) =>
        Strategies.TryGetValue(profile.Type.Trim(), out var strategy)
            ? strategy
            : throw new InvalidOperationException($"Unsupported auth profile type '{profile.Type}'.");
}

internal sealed class NoAuthenticationStrategy : IApiAuthenticationStrategy
{
    public string Name => "none";

    public void Apply(
        HttpRequestMessage message,
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader)
    {
    }

    public string DescribeTokenState(
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader) => "not applicable";
}

internal sealed class BearerAuthenticationStrategy : IApiAuthenticationStrategy
{
    public string Name => "bearer";

    public void Apply(
        HttpRequestMessage message,
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader)
    {
        var token = tokenReader(TokenVariable(profile, settings));
        if (!string.IsNullOrWhiteSpace(token))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public string DescribeTokenState(
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader)
    {
        var variable = TokenVariable(profile, settings);
        return string.IsNullOrWhiteSpace(tokenReader(variable))
            ? $"missing ({variable})"
            : "bearer token available";
    }

    private static string TokenVariable(AuthProfileSettings profile, ApiSettingsSnapshot settings) =>
        string.IsNullOrWhiteSpace(profile.TokenEnvironmentVariable)
            ? settings.TokenEnvironmentVariable
            : profile.TokenEnvironmentVariable;
}

internal sealed class ApiKeyAuthenticationStrategy : IApiAuthenticationStrategy
{
    public string Name => "apikey";

    public void Apply(
        HttpRequestMessage message,
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader)
    {
        var key = tokenReader(TokenVariable(profile, settings));
        if (!string.IsNullOrWhiteSpace(key))
        {
            var header = string.IsNullOrWhiteSpace(profile.HeaderName) ? "X-API-Key" : profile.HeaderName;
            message.Headers.TryAddWithoutValidation(header, key);
        }
    }

    public string DescribeTokenState(
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader)
    {
        var variable = TokenVariable(profile, settings);
        return string.IsNullOrWhiteSpace(tokenReader(variable))
            ? $"missing ({variable})"
            : "api key available";
    }

    private static string TokenVariable(AuthProfileSettings profile, ApiSettingsSnapshot settings) =>
        string.IsNullOrWhiteSpace(profile.TokenEnvironmentVariable)
            ? settings.TokenEnvironmentVariable
            : profile.TokenEnvironmentVariable;
}

internal sealed class BasicAuthenticationStrategy : IApiAuthenticationStrategy
{
    public string Name => "basic";

    public void Apply(
        HttpRequestMessage message,
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader)
    {
        var usernameVariable = profile.UsernameEnvironmentVariable ?? "ATF_API_USERNAME";
        var passwordVariable = profile.PasswordEnvironmentVariable ?? "ATF_API_PASSWORD";
        var username = tokenReader(usernameVariable);
        var password = tokenReader(passwordVariable);
        if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
        {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        }
    }

    public string DescribeTokenState(
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader)
    {
        var usernameVariable = profile.UsernameEnvironmentVariable ?? "ATF_API_USERNAME";
        var passwordVariable = profile.PasswordEnvironmentVariable ?? "ATF_API_PASSWORD";
        return string.IsNullOrWhiteSpace(tokenReader(usernameVariable)) ||
               string.IsNullOrWhiteSpace(tokenReader(passwordVariable))
            ? $"missing ({usernameVariable}/{passwordVariable})"
            : "basic credentials available";
    }
}

internal sealed class CustomHeadersAuthenticationStrategy : IApiAuthenticationStrategy
{
    public string Name => "custom";

    public void Apply(
        HttpRequestMessage message,
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader)
    {
        foreach (var header in profile.Headers)
        {
            if (!string.IsNullOrWhiteSpace(header.Key) && header.Value is not null)
            {
                message.Headers.TryAddWithoutValidation(header.Key, ResolveConfiguredValue(header.Value, tokenReader));
            }
        }
    }

    public string DescribeTokenState(
        AuthProfileSettings profile,
        ApiSettingsSnapshot settings,
        Func<string, string?> tokenReader) =>
        profile.Headers.Count == 0 ? "custom headers not configured" : "custom headers configured";

    private static string ResolveConfiguredValue(string value, Func<string, string?> tokenReader) =>
        value.StartsWith("${", StringComparison.Ordinal) && value.EndsWith('}')
            ? tokenReader(value[2..^1]) ?? string.Empty
            : value;
}
