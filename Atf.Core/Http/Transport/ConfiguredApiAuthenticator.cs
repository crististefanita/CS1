using Atf.Core.Configuration;
using Atf.Core.Http.Transport.Authentication;

namespace Atf.Core.Http.Transport;

public sealed class ConfiguredApiAuthenticator : IApiAuthenticator
{
    private readonly ApiSettingsSnapshot _settings;
    private readonly Func<string, string?> _tokenReader;

    public ConfiguredApiAuthenticator(ApiSettings settings, Func<string, string?>? tokenReader = null)
    {
        _settings = settings?.ToRuntimeSnapshot() ?? throw new ArgumentNullException(nameof(settings));
        _tokenReader = tokenReader ?? Environment.GetEnvironmentVariable;
    }

    internal ConfiguredApiAuthenticator(ApiSettingsSnapshot settings, Func<string, string?>? tokenReader = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _tokenReader = tokenReader ?? Environment.GetEnvironmentVariable;
    }

    public void Apply(HttpRequestMessage message, string profileName)
    {
        var profile = GetProfile(profileName);
        ApiAuthenticationStrategyCatalog.Resolve(profile).Apply(message, profile, _settings, _tokenReader);
    }

    public string DescribeMode(string profileName) => ApiAuthenticationStrategyCatalog.Resolve(GetProfile(profileName)).Name;

    public string DescribeTokenState(string profileName)
    {
        var profile = GetProfile(profileName);
        return ApiAuthenticationStrategyCatalog.Resolve(profile)
            .DescribeTokenState(profile, _settings, _tokenReader);
    }

    private AuthProfileSettings GetProfile(string profileName) =>
        _settings.AuthProfiles.TryGetValue(profileName, out var profile)
            ? profile
            : throw new InvalidOperationException($"Auth profile '{profileName}' is not configured.");
}
