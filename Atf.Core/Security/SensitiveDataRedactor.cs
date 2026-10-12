using Atf.Core.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Atf.Core.Security;

/// <summary>
/// Central report-boundary redaction. Secrets are masked before evidence
/// reaches any reporter while HTTP execution continues to use original data.
/// </summary>
public sealed class SensitiveDataRedactor
{
    private readonly RedactionSettings _settings;
    private readonly HashSet<string> _sensitiveKeys;

    public SensitiveDataRedactor(RedactionSettings? settings = null)
    {
        _settings = settings ?? new RedactionSettings();
        _sensitiveKeys = _settings.SensitiveKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public string RedactField(string field, string? value)
    {
        value ??= string.Empty;
        if (!_settings.Enabled)
        {
            return value;
        }

        return IsSensitiveField(field) ? _settings.Replacement : RedactQueryString(value);
    }

    public string RedactJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || !_settings.Enabled)
        {
            return json ?? string.Empty;
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return json;
            }

            RedactNode(node);
            return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private void RedactNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (IsSensitiveField(property.Key))
                {
                    obj[property.Key] = _settings.Replacement;
                }
                else if (property.Value is not null)
                {
                    RedactNode(property.Value);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is not null)
                {
                    RedactNode(item);
                }
            }
        }
    }

    private bool IsSensitiveField(string field) => _sensitiveKeys.Contains(Normalize(field));

    private string RedactQueryString(string value)
    {
        if (!value.Contains('?', StringComparison.Ordinal) && !value.Contains('&', StringComparison.Ordinal))
        {
            return value;
        }

        var keyPattern = string.Join("|", _sensitiveKeys.Select(Regex.Escape));
        return Regex.Replace(
            value,
            $"(?<prefix>[?&](?:{keyPattern})=)[^&]*",
            $"${{prefix}}{_settings.Replacement}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string Normalize(string value) =>
        value.Trim().Replace('-', '_').ToLowerInvariant();
}
