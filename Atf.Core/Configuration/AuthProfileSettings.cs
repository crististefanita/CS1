namespace Atf.Core.Configuration.Models;

public sealed class AuthProfileSettings
{
    public string Type { get; init; } = "none";
    public string? TokenEnvironmentVariable { get; init; }
    public string? HeaderName { get; init; }
    public string? UsernameEnvironmentVariable { get; init; }
    public string? PasswordEnvironmentVariable { get; init; }
    public Dictionary<string, string?> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public AuthProfileSettings Snapshot() => new()
    {
        Type = Type,
        TokenEnvironmentVariable = TokenEnvironmentVariable,
        HeaderName = HeaderName,
        UsernameEnvironmentVariable = UsernameEnvironmentVariable,
        PasswordEnvironmentVariable = PasswordEnvironmentVariable,
        Headers = Headers.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
    };
}
