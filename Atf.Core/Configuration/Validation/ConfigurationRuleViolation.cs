namespace Atf.Core.Configuration.Validation;

using Atf.Core.Configuration;

/// <summary>
/// Centralizes the stable exception construction used by configuration rules.
/// Keeping this internal helper in the validation boundary prevents the public
/// configuration namespace from becoming a catch-all for implementation types.
/// </summary>
internal static class ConfigurationRuleViolation
{
    public static void Throw(string code, string path, string message) =>
        throw new AtfConfigurationException(code, path, message);
}
