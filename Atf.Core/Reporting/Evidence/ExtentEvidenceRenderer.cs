using System.Net;

namespace Atf.Core.Reporting.Evidence;

/// <summary>
/// Owns the small HTML envelope used for Extent evidence. The scope decides
/// what happened; this renderer decides how that evidence is represented.
/// </summary>
public sealed class ExtentEvidenceRenderer
{
    public string JsonDisplayLabel(string label)
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

    public string Build(
        string title,
        string body,
        string kind = "",
        bool expanded = false)
    {
        var cssClass = string.IsNullOrWhiteSpace(kind) ? "step-detail" : $"step-detail {kind}";
        var open = expanded ? " open" : string.Empty;

        return $"<details class='{cssClass}'{open}><summary>" +
               "<span class='se-toggle se-open'>[+] expand</span>" +
               "<span class='se-toggle se-close'>[-] collapse</span>" +
               $"<span class='se-title'>{title}</span></summary><div class='body'>{body}</div></details>";
    }

    public string Html(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
}

