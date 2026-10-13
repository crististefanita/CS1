using Atf.Core.Configuration.Security;
using System.Text.Json;

namespace Atf.Core.Security.Redaction;

internal sealed class RedactionContext
{
    private RedactionContext(
        RedactionSettings settings,
        HashSet<string> sensitiveKeys,
        HashSet<string> piiKeys)
    {
        Settings = settings;
        SensitiveKeys = sensitiveKeys;
        PiiKeys = piiKeys;
    }

    public RedactionSettings Settings { get; }
    public HashSet<string> SensitiveKeys { get; }
    public HashSet<string> PiiKeys { get; }
    public string Replacement => Settings.Replacement;

    public static JsonSerializerOptions PrettyJsonOptions { get; } = new() { WriteIndented = true };

    public static RedactionContext Create(RedactionSettings settings) =>
        new(
            settings,
            settings.SensitiveKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(Normalize)
                .ToHashSet(StringComparer.OrdinalIgnoreCase),
            settings.PiiKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(Normalize)
                .ToHashSet(StringComparer.OrdinalIgnoreCase));

    public bool IsSensitiveField(string field)
    {
        var key = Normalize(field);
        return SensitiveKeys.Contains(key) || (Settings.PiiEnabled && PiiKeys.Contains(key));
    }

    public IEnumerable<string> ReportKeys() => Settings.PiiEnabled
        ? SensitiveKeys.Concat(PiiKeys).Distinct(StringComparer.OrdinalIgnoreCase)
        : SensitiveKeys;

    public static string Normalize(string value) =>
        value.Trim().Replace('-', '_').ToLowerInvariant();
}
