using System.Text.RegularExpressions;

namespace Atf.Core.Security.Redaction;

internal sealed class PiiRedactor
{
    private static readonly Regex EmailPattern = new(
        @"(?<![\w.+-])[\w.+-]+@[\w-]+(?:\.[\w-]+)+(?![\w.-])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly RedactionContext _context;

    public PiiRedactor(RedactionContext context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    public string Apply(string value) =>
        EmailPattern.Replace(value, _context.Replacement);
}
