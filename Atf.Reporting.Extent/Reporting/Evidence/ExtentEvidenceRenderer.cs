using System.Net;

namespace Atf.Reporting.Extent.Evidence;

/// <summary>
/// Owns the small HTML envelope used for Extent evidence. The scope decides
/// what happened; this renderer decides how that evidence is represented.
/// </summary>
public sealed class ExtentEvidenceRenderer
{
    public static string JsonDisplayLabel(string label)
    {
        if (label.Contains("request", StringComparison.OrdinalIgnoreCase))
        {
            return "REQUEST JSON";
        }

        if (label.Contains("response", StringComparison.OrdinalIgnoreCase))
        {
            return "RESPONSE JSON";
        }

        return string.IsNullOrWhiteSpace(label) ? "JSON BODY" : label.ToUpperInvariant();
    }

    public static string Build(
        string title,
        string body,
        string kind = "",
        bool expanded = false)
    {
        var cssClass = string.IsNullOrWhiteSpace(kind) ? "step-detail" : $"step-detail {kind}";
        var open = expanded ? " open" : string.Empty;
        var safeTitle = Html(title.Trim());

        return $"<details class='{cssClass}'{open}><summary>" +
               "<span class='se-toggle se-open'>[+] expand</span>" +
               "<span class='se-toggle se-close'>[-] collapse</span>" +
               $"<span class='se-title' title='{safeTitle}' aria-label='{safeTitle}'>{safeTitle}</span></summary><div class='body'>{body}</div></details>";
    }

    public static string Html(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
}

