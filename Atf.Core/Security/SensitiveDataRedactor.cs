using Atf.Core.Configuration.Security;
using Atf.Core.Security.Redaction;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Atf.Core.Security;

/// <summary>
/// Composes the report-boundary redaction policies. The facade owns ordering;
/// each concrete transformation has one responsibility and can be tested in
/// isolation without coupling the reporting boundary to regex or JSON details.
/// </summary>
public sealed class SensitiveDataRedactor
{
    private readonly RedactionContext _context;
    private readonly SecretPatternRedactor _secretPatterns;
    private readonly QueryStringRedactor _queryStrings;
    private readonly LocalPathRedactor _localPaths;
    private readonly PiiRedactor _pii;
    private readonly StructuredJsonRedactor _structuredJson;

    public SensitiveDataRedactor(RedactionSettings? settings = null)
    {
        _context = RedactionContext.Create(settings ?? new RedactionSettings());
        _secretPatterns = new(_context);
        _queryStrings = new(_context);
        _localPaths = new(_context);
        _pii = new(_context);
        _structuredJson = new(_context, RedactText);
    }

    public string RedactField(string field, string? value)
    {
        value ??= string.Empty;
        if (_context.IsSensitiveField(field))
        {
            return _context.Replacement;
        }

        var querySafe = _queryStrings.Apply(value);
        return !_context.Settings.Enabled ? querySafe : RedactText(querySafe);
    }

    /// <summary>
    /// Redacts free-form diagnostics as a final safety net. This is used for
    /// exception text and retry messages where no structured field name exists.
    /// </summary>
    public string RedactText(string? value)
    {
        value ??= string.Empty;
        if (value.Length == 0)
        {
            return value;
        }

        var redacted = _secretPatterns.Apply(value);
        if (!_context.Settings.Enabled)
        {
            return redacted;
        }

        if (_context.Settings.RedactLocalPaths)
        {
            redacted = _localPaths.Apply(redacted);
        }

        return _context.Settings.PiiEnabled ? _pii.Apply(redacted) : redacted;
    }

    public string RedactJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
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

            _structuredJson.Apply(node);
            return node.ToJsonString(RedactionContext.PrettyJsonOptions);
        }
        catch (JsonException)
        {
            // A malformed or plain-text body still belongs to the evidence
            // boundary; never bypass redaction because it is not JSON.
            return RedactText(json);
        }
    }
}
