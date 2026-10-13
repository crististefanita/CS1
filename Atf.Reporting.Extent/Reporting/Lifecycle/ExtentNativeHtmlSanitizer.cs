namespace Atf.Reporting.Extent.Lifecycle.Sanitization;

/// <summary>
/// Removes ATF-owned fragments from a native Extent document and normalizes the
/// native body theme before ATF-owned resources are injected.
/// </summary>
internal static class ExtentNativeHtmlSanitizer
{
    public static string RemoveAtfOwnedBlocks(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"\s*<style\s+id=[""']atf-custom-theme[""'][^>]*>.*?</style>\s*",
            Environment.NewLine,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        return System.Text.RegularExpressions.Regex.Replace(
            html,
            @"\s*<script\s+id=[""']atf-custom-behaviour[""'][^>]*>.*?</script>\s*",
            Environment.NewLine,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
    }

    public static string EnsureDarkExtentTheme(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        return System.Text.RegularExpressions.Regex.Replace(
            html,
            "class=([\"'])spa(\\s+)standard\\1",
            "class=$1spa$2dark$1",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
