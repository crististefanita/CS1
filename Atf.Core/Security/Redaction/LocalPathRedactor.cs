using System.Text.RegularExpressions;

namespace Atf.Core.Security.Redaction;

internal sealed class LocalPathRedactor
{
    private static readonly Regex WindowsAbsolutePathPattern = new(
        @"(?<![\w])(?<path>(?:[A-Za-z]:\\|\\\\)[^\r\n""']+?)(?=(?::line\s+\d+|[\)\],;]|$))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly RedactionContext _context;

    public LocalPathRedactor(RedactionContext context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    public string Apply(string value) =>
        WindowsAbsolutePathPattern.Replace(value, _context.Settings.LocalPathReplacement);
}
