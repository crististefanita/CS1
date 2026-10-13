using System.Net;
using System.Text.RegularExpressions;

namespace Atf.Reporting.Extent.Lifecycle.Sanitization;

/// <summary>
/// Keeps Extent's source-level tag selector aligned with the configured ATF
/// taxonomy. Browser JavaScript still owns hydration and annotation layout;
/// this collaborator guarantees a correct standalone HTML contract first.
/// </summary>
internal static partial class ExtentNativeTagSelectorSanitizer
{
    public static string KeepSelectionTags(string html, AtfTagTaxonomy taxonomy)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(taxonomy);

        return TagMenuRegex().Replace(html, match =>
        {
            var body = match.Groups["body"].Value;
            var filtered = TagItemRegex().Replace(body, item =>
            {
                var text = WebUtility.HtmlDecode(StripMarkupRegex().Replace(item.Value, string.Empty)).Trim();
                return taxonomy.Classify(text) == AtfTagKind.Selection ? item.Value : string.Empty;
            });

            return match.Value.Replace(body, filtered, StringComparison.Ordinal);
        });
    }

    [GeneratedRegex("<ul\\b[^>]*\\bid=(['\"])tag-toggle\\1[^>]*>(?<body>.*?)</ul>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex TagMenuRegex();

    [GeneratedRegex("\\s*<a\\b[^>]*\\bclass=(['\"])['\"]?[^>]*\\bdropdown-item\\b[^>]*>.*?</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex TagItemRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex StripMarkupRegex();
}
