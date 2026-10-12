using AventStack.ExtentReports;
using System.Reflection;

namespace Atf.Reporting.Extent.Artifacts;

/// <summary>
/// Converts Extent's offline asset references into embedded content so the
/// delivered report remains a single portable HTML artifact.
/// </summary>
public sealed class ExtentAssetInliner
{
    public static string Inline(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var assembly = typeof(ExtentReports).Assembly;
        var sparkCss = ReadEmbeddedText(assembly, "AventStack.ExtentReports.Views.Spark.Offline.spark-style.css");
        var fontAwesomeCss = ReadEmbeddedText(assembly, "AventStack.ExtentReports.Views.Commons.Offline.font-awesome.min.css");
        var jsonTreeJs = ReadEmbeddedText(assembly, "AventStack.ExtentReports.Views.Commons.Offline.jsontree.js");
        var sparkScriptJs = ReadEmbeddedText(assembly, "AventStack.ExtentReports.Views.Spark.Offline.spark-script.js");

        foreach (var extension in new[] { "eot", "woff2", "woff", "ttf", "svg" })
        {
            var resource = $"AventStack.ExtentReports.Views.Commons.Offline.fontawesome-webfont.{extension}";
            var mediaType = extension switch
            {
                "eot" => "application/vnd.ms-fontobject",
                "woff2" => "font/woff2",
                "woff" => "font/woff",
                "ttf" => "font/ttf",
                _ => "image/svg+xml"
            };
            var dataUri = ReadEmbeddedDataUri(assembly, resource, mediaType);
            var fileName = $"fontawesome-webfont.{extension}";

            if (extension.Equals("eot", StringComparison.OrdinalIgnoreCase))
            {
                fontAwesomeCss = fontAwesomeCss.Replace(
                    $"https://extentreports.s3.us-east-2.amazonaws.com/dist/v5/avent/{fileName}?#iefix&v=4.7.0",
                    $"{dataUri}#iefix",
                    StringComparison.OrdinalIgnoreCase);
            }

            fontAwesomeCss = fontAwesomeCss.Replace(
                $"https://extentreports.s3.us-east-2.amazonaws.com/dist/v5/avent/{fileName}?v=4.7.0",
                dataUri,
                StringComparison.OrdinalIgnoreCase);

            fontAwesomeCss = fontAwesomeCss.Replace(
                $"https://extentreports.s3.us-east-2.amazonaws.com/dist/v5/avent/{fileName}?v=4.7.0#fontawesomeregular",
                $"{dataUri}#fontawesomeregular",
                StringComparison.OrdinalIgnoreCase);
        }

        var logo = ReadEmbeddedDataUri(
            assembly,
            "AventStack.ExtentReports.Views.Commons.Offline.logo.png",
            "image/png");

        return html
            .Replace("extent/logo.png", logo, StringComparison.OrdinalIgnoreCase)
            .Replace(
                "<link href=\"extent/spark-style.css\" rel=\"stylesheet\" />",
                $"<style id=\"atf-extent-spark-style\">{sparkCss}</style>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "<link href=\"extent/font-awesome.min.css\" rel=\"stylesheet\">",
                $"<style id=\"atf-extent-font-awesome\">{fontAwesomeCss}</style>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "<script src=\"extent/jsontree.js\"></script>",
                $"<script id=\"atf-extent-json-tree\">{jsonTreeJs}</script>",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "<script src=\"extent/spark-script.js\"></script>",
                $"<script id=\"atf-extent-spark-script\">{sparkScriptJs}</script>",
                StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadEmbeddedText(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded Extent resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ReadEmbeddedDataUri(Assembly assembly, string resourceName, string mediaType)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded Extent resource: {resourceName}");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return $"data:{mediaType};base64,{Convert.ToBase64String(buffer.ToArray())}";
    }
}
