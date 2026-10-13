using System.Text.RegularExpressions;

namespace Atf.Core.Security.Redaction;

internal sealed class QueryStringRedactor
{
    private readonly RedactionContext _context;

    public QueryStringRedactor(RedactionContext context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    public string Apply(string value)
    {
        if (!value.Contains('?', StringComparison.Ordinal) &&
            !value.Contains('&', StringComparison.Ordinal))
        {
            return value;
        }

        var keyPattern = string.Join("|", _context.ReportKeys().Select(Regex.Escape));
        if (keyPattern.Length == 0)
        {
            return value;
        }

        return Regex.Replace(
            value,
            $"(?<prefix>[?&](?:{keyPattern})=)[^&]*",
            $"${{prefix}}{_context.Replacement}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
