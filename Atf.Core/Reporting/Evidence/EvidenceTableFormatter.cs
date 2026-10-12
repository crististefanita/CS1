using System.Net;
using System.Globalization;

namespace Atf.Core.Reporting.Evidence;

/// <summary>
/// Stable renderer for diagnostic field/value evidence such as technical traces.
/// </summary>
public static class EvidenceTableFormatter
{
    private const decimal MeaningfulExchangeDeltaMs = 50m;

    private static readonly IReadOnlyDictionary<string, string> FriendlyNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["route.name"] = "ROUTE",
            ["route.resource"] = "RESOURCE",
            ["route.operation"] = "OPERATION",
            ["route.path.template"] = "ROUTE TEMPLATE",
            ["route.auth.type"] = "ROUTE AUTH TYPE",
            ["route.auth.requirement"] = "ROUTE AUTH ACCESS",
            ["route.auth.profile"] = "ROUTE AUTH PROFILE",
            ["route.auth.policy"] = "ROUTE AUTH POLICY",
            ["request.type"] = "REQUEST TYPE",
            ["method"] = "METHOD",
            ["path"] = "PATH",
            ["url"] = "ENDPOINT",
            ["auth.profile"] = "AUTHENTICATION",
            ["auth.mode"] = "AUTH MODE",
            ["auth.token.state"] = "TOKEN STATE",
            ["attempt"] = "ATTEMPT",
            ["response.status"] = "RESPONSE STATUS",
            ["response.content.type"] = "CONTENT TYPE",
            ["response.duration.ms"] = "RESPONSE DURATION",
            ["exchange.duration.ms"] = "TOTAL DURATION",
            ["rate-limit.retry.delay.ms"] = "RETRY DELAY",
            ["rate-limit.retry.reason"] = "RETRY REASON"
        };

    /// <summary>
    /// Reduces the raw executor diagnostics to the fields that explain the
    /// exchange. The route template and attempt number remain explicit so the
    /// request can be understood without the raw executor trace. Route-auth
    /// policy metadata belongs to the request contract, not to every response
    /// trace; keeping it out here preserves the compact eight-field contract
    /// used by the reference Extent report. Retry-only fields are retained
    /// when a retry occurred.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, string>> CompactTechnicalTraceDetails(
        IReadOnlyCollection<KeyValuePair<string, string>> details)
    {
        var values = details
            .GroupBy(detail => detail.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);
        var compact = new List<KeyValuePair<string, string>>();

        Add("method");
        if (values.ContainsKey("url"))
        {
            Add("url");
        }
        else
        {
            Add("path");
        }

        Add("route.path.template");
        if (values.ContainsKey("auth.profile"))
        {
            Add("auth.profile");
        }
        else
        {
            Add("auth.mode");
        }

        Add("response.status");
        Add("response.content.type");
        Add("response.duration.ms");

        var attempt = TryInt("attempt");
        if (attempt is not null)
        {
            Add("attempt");
        }

        var responseDuration = TryDecimal("response.duration.ms");
        var exchangeDuration = TryDecimal("exchange.duration.ms");
        if (exchangeDuration is not null &&
            (attempt is > 1 ||
             responseDuration is null ||
             Math.Abs(exchangeDuration.Value - responseDuration.Value) > MeaningfulExchangeDeltaMs))
        {
            Add("exchange.duration.ms");
        }

        Add("rate-limit.retry.delay.ms");
        Add("rate-limit.retry.reason");
        return compact;

        void Add(string key)
        {
            if (values.TryGetValue(key, out var value) &&
                !compact.Any(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                compact.Add(new(key, value));
            }
        }

        int? TryInt(string key) => values.TryGetValue(key, out var value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

        decimal? TryDecimal(string key) => values.TryGetValue(key, out var value) &&
            decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    public static string BuildTechnicalTraceTableHtml(
        IReadOnlyCollection<KeyValuePair<string, string>> details)
        => BuildTechnicalTraceTableHtml(details, compact: true);

    public static string BuildTechnicalTraceTableHtml(
        IReadOnlyCollection<KeyValuePair<string, string>> details,
        bool compact)
    {
        var compactDetails = compact
            ? CompactTechnicalTraceDetails(details)
            : details.ToArray();
        var rows = string.Join(
            string.Empty,
            compactDetails.Select(detail =>
                $"<tr><th scope='row'>{Html(DisplayName(detail.Key))}</th><td data-field='{Html(detail.Key)}'>{Html(detail.Value)}</td></tr>"));

        return "<table class='helm-evidence-table helm-trace-table'>" +
            "<thead><tr><th>FIELD</th><th>VALUE</th></tr></thead>" +
            $"<tbody>{rows}</tbody></table>";
    }

    private static string DisplayName(string key) =>
        FriendlyNames.TryGetValue(key, out var name)
            ? name
            : key.Replace('.', ' ').Replace('_', ' ').ToUpperInvariant();

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
