using System.Text.RegularExpressions;

namespace Atf.Core.Security.Redaction;

internal sealed class SecretPatternRedactor
{
    private static readonly Regex AuthorizationPattern = new(
        @"(?<prefix>\b(?:Bearer|Basic|Token|Api[- ]?Key)\s+)(?!(?:required|available|missing|not|none|token)\b)[^\s,;]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex HeaderPattern = new(
        @"(?<prefix>\b(?:Authorization|Proxy-Authorization|X-Api-Key|Api-Key|Cookie|Set-Cookie)\s*[:=]\s*)[^\r\n,;]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly RedactionContext _context;

    public SecretPatternRedactor(RedactionContext context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    public string Apply(string value)
    {
        var redacted = AuthorizationPattern.Replace(
            value,
            match => match.Groups["prefix"].Value + _context.Replacement);
        redacted = HeaderPattern.Replace(
            redacted,
            match => match.Groups["prefix"].Value + _context.Replacement);

        var keyPattern = string.Join("|", _context.ReportKeys().Select(Regex.Escape));
        if (keyPattern.Length == 0)
        {
            return redacted;
        }

        return Regex.Replace(
            redacted,
            $@"(?<prefix>\b(?:{keyPattern})\s*[:=]\s*)[^\s,;&]+",
            $"${{prefix}}{_context.Replacement}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
