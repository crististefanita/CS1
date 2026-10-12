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
    private static readonly Regex EmailPattern = new(
        @"(?<![\w.+-])[\w.+-]+@[\w-]+(?:\.[\w-]+)+(?![\w.-])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex AuthorizationPattern = new(
        @"(?<prefix>\b(?:Bearer|Basic|Token|Api[- ]?Key)\s+)[^\s,;]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex HeaderPattern = new(
        @"(?<prefix>\b(?:Authorization|Proxy-Authorization|X-Api-Key|Api-Key|Cookie|Set-Cookie)\s*[:=]\s*)[^\r\n,;]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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

        return IsSensitiveField(field) ? _settings.Replacement : RedactText(RedactQueryString(value));
    }

    /// <summary>
    /// Redacts free-form diagnostics as a final safety net. This is used for
    /// exception text and retry messages where no structured field name exists.
    /// </summary>
    public string RedactText(string? value)
    {
        value ??= string.Empty;
        if (!_settings.Enabled || value.Length == 0)
        {
            return value;
        }

        var redacted = AuthorizationPattern.Replace(value, match => match.Groups["prefix"].Value + _settings.Replacement);
        redacted = HeaderPattern.Replace(redacted, match => match.Groups["prefix"].Value + _settings.Replacement);
        return EmailPattern.Replace(redacted, _settings.Replacement);
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
            // A malformed or plain-text body still belongs to the evidence
            // boundary; never bypass redaction just because it is not JSON.
            return RedactText(json);
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
                else if (property.Value is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    obj[property.Key] = RedactText(text);
                }
                else if (property.Value is not null)
                {
                    RedactNode(property.Value);
                }
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                var item = array[index];
                if (item is not null)
                {
                    if (item is JsonValue value && value.TryGetValue<string>(out var text))
                    {
                        array[index] = RedactText(text);
                    }
                    else
                    {
                        RedactNode(item);
                    }
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
        var redacted = Regex.Replace(
            value,
            $"(?<prefix>[?&](?:{keyPattern})=)[^&]*",
            $"${{prefix}}{_settings.Replacement}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return RedactText(redacted);
    }

    private static string Normalize(string value) =>
        value.Trim().Replace('-', '_').ToLowerInvariant();
}
