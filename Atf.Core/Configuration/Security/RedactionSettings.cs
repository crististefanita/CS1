namespace Atf.Core.Configuration.Security;

/// <summary>
/// Controls what is masked at the reporting boundary. Secrets are always
/// opt-in by key; PII keys can be removed by a module when they are needed for
/// business diagnostics.
/// </summary>
public sealed class RedactionSettings
{
    public bool Enabled { get; init; } = true;
    public string Replacement { get; init; } = "[REDACTED]";
    public bool RedactLocalPaths { get; init; } = true;
    public string LocalPathReplacement { get; init; } = "[LOCAL_PATH]";
    public string[] SensitiveKeys { get; init; } =
    [
        "access_token", "api_key", "apikey", "authorization", "client_secret",
        "password", "refresh_token", "secret", "token"
    ];
    public bool PiiEnabled { get; init; }
    public string[] PiiKeys { get; init; } =
    ["email", "email_address", "phone", "phone_number"];

    public RedactionSettings Snapshot() => new()
    {
        Enabled = Enabled,
        Replacement = Replacement,
        RedactLocalPaths = RedactLocalPaths,
        LocalPathReplacement = LocalPathReplacement,
        SensitiveKeys = SensitiveKeys.ToArray(),
        PiiEnabled = PiiEnabled,
        PiiKeys = PiiKeys.ToArray()
    };
}
