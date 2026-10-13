namespace Atf.Reporting.Extent.Lifecycle;

/// <summary>
/// Removes native overview surfaces that conflict with ATF's semantic overview
/// vocabulary. Structural removal keeps source HTML and hydrated DOM aligned.
/// </summary>
internal static class ExtentNativeOverviewPruner
{
    public static string RemoveNativeLogEventsCard(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        const string title = "Log events";
        var titleIndex = html.IndexOf(title, StringComparison.OrdinalIgnoreCase);
        if (titleIndex < 0)
        {
            return html;
        }

        var columns = System.Text.RegularExpressions.Regex.Matches(
            html,
            "<div\\b[^>]*class\\s*=\\s*[\\\"'][^\\\"']*\\bcol-md-3\\b[^\\\"']*[\\\"'][^>]*>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var column = columns.Cast<System.Text.RegularExpressions.Match>()
            .LastOrDefault(match => match.Index < titleIndex);
        if (column is null)
        {
            return html;
        }

        var end = FindMatchingDivEnd(html, column.Index);
        return end < 0
            ? html
            : html.Remove(column.Index, end - column.Index);
    }

    public static string RemoveNativeEventsChart(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        return System.Text.RegularExpressions.Regex.Replace(
            html,
            @"\s*/\*\s*--\s*\[\s*events chart\s*\]\s*--\s*\*/\s*\(function\s+drawEventsChart\(\)\s*\{.*?\}\)\(\);\s*",
            Environment.NewLine,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Singleline |
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }

    private static int FindMatchingDivEnd(string html, int start)
    {
        var depth = 0;
        var cursor = start;
        while (cursor < html.Length)
        {
            var open = html.IndexOf("<div", cursor, StringComparison.OrdinalIgnoreCase);
            var close = html.IndexOf("</div", cursor, StringComparison.OrdinalIgnoreCase);
            if (close < 0)
            {
                return -1;
            }

            if (open >= 0 && open < close)
            {
                depth++;
                cursor = open + 4;
                continue;
            }

            depth--;
            var closeEnd = html.IndexOf('>', close);
            if (closeEnd < 0)
            {
                return -1;
            }

            if (depth == 0)
            {
                return closeEnd + 1;
            }

            cursor = closeEnd + 1;
        }

        return -1;
    }
}
