using System.Text.Json.Nodes;

namespace Atf.Core.Security.Redaction;

internal sealed class StructuredJsonRedactor
{
    private readonly RedactionContext _context;
    private readonly Func<string, string> _redactText;

    public StructuredJsonRedactor(RedactionContext context, Func<string, string> redactText)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _redactText = redactText ?? throw new ArgumentNullException(nameof(redactText));
    }

    public void Apply(JsonNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ApplyNode(node);
    }

    private void ApplyNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (_context.IsSensitiveField(property.Key))
                {
                    obj[property.Key] = _context.Replacement;
                }
                else if (property.Value is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    obj[property.Key] = _redactText(text);
                }
                else if (property.Value is not null)
                {
                    ApplyNode(property.Value);
                }
            }

            return;
        }

        if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                var item = array[index];
                if (item is null)
                {
                    continue;
                }

                if (item is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    array[index] = _redactText(text);
                }
                else
                {
                    ApplyNode(item);
                }
            }
        }
    }
}
