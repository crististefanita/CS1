using System.Globalization;
using System.Net;

namespace Atf.Reporting.Extent.Evidence;

/// <summary>
/// Defines how a compact technical trace handles repeated field names.
/// </summary>
public enum DuplicateTechnicalTraceKeyPolicy
{
    KeepLast,
    KeepFirst,
    KeepAll
}

/// <summary>
/// Extent-specific renderer for diagnostic field/value evidence.
/// </summary>
public static class EvidenceTableFormatter
{
    private const decimal MeaningfulExchangeDeltaMs = 50m;

    private static readonly Dictionary<string, string> FriendlyNames =
        new(StringComparer.OrdinalIgnoreCase)
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
            ["response.duration.ms"] = "RESPONSE DURATION (ms)",
            ["exchange.duration.ms"] = "TOTAL DURATION (ms)",
            ["rate-limit.retry.delay.ms"] = "RETRY DELAY",
            ["rate-limit.retry.reason"] = "RETRY REASON"
        };

    /// <summary>
    /// Reduces executor diagnostics to the fields that explain the exchange.
    /// The final exchange is the default view; diagnostic callers can retain
    /// the first or every occurrence explicitly.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, string>> CompactTechnicalTraceDetails(
        IReadOnlyCollection<KeyValuePair<string, string>> details,
        DuplicateTechnicalTraceKeyPolicy duplicateKeyPolicy = DuplicateTechnicalTraceKeyPolicy.KeepLast)
    {
        ArgumentNullException.ThrowIfNull(details);

        var values = details
            .GroupBy(detail => detail.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(detail => detail.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);
        var compact = new List<KeyValuePair<string, string>>();

        Add("method");
        if (values.ContainsKey("url")) Add("url"); else Add("path");
        Add("route.path.template");
        if (values.ContainsKey("auth.profile")) Add("auth.profile"); else Add("auth.mode");
        Add("response.status");
        Add("response.content.type");
        Add("response.duration.ms");

        var attempt = TryInt("attempt");
        if (attempt is not null) Add("attempt");

        var responseDuration = TryDecimal("response.duration.ms");
        var exchangeDuration = TryDecimal("exchange.duration.ms");
        if (exchangeDuration is not null &&
            (attempt is > 1 || responseDuration is null ||
             Math.Abs(exchangeDuration.Value - responseDuration.Value) > MeaningfulExchangeDeltaMs))
        {
            Add("exchange.duration.ms");
        }

        Add("rate-limit.retry.delay.ms");
        Add("rate-limit.retry.reason");
        return compact;

        void Add(string key)
        {
            if (!values.TryGetValue(key, out var occurrences)) return;

            var selected = duplicateKeyPolicy switch
            {
                DuplicateTechnicalTraceKeyPolicy.KeepFirst => occurrences.Take(1),
                DuplicateTechnicalTraceKeyPolicy.KeepAll => occurrences,
                _ => occurrences.TakeLast(1)
            };

            foreach (var value in selected)
            {
                if (duplicateKeyPolicy != DuplicateTechnicalTraceKeyPolicy.KeepAll &&
                    compact.Any(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase))) break;
                compact.Add(new(key, value));
            }
        }

        int? TryInt(string key) => values.TryGetValue(key, out var occurrences) &&
            int.TryParse(occurrences[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed : null;

        decimal? TryDecimal(string key) => values.TryGetValue(key, out var occurrences) &&
            decimal.TryParse(occurrences[^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed : null;
    }

    public static string BuildTechnicalTraceTableHtml(
        IReadOnlyCollection<KeyValuePair<string, string>> details) =>
        BuildTechnicalTraceTableHtml(details, compact: true);

    public static string BuildTechnicalTraceTableHtml(
        IReadOnlyCollection<KeyValuePair<string, string>> details,
        bool compact)
    {
        var compactDetails = compact ? CompactTechnicalTraceDetails(details) : details.ToArray();
        var rows = string.Join(string.Empty, compactDetails.Select(detail =>
            $"<tr><th scope='row'>{Html(DisplayName(detail.Key))}</th><td data-field='{Html(detail.Key)}'>{Html(detail.Value)}</td></tr>"));

        return "<div class='helm-evidence-table-wrap helm-trace-table-wrap'>" +
            "<table class='helm-evidence-table helm-trace-table'>" +
            "<thead><tr><th>FIELD</th><th>VALUE</th></tr></thead>" +
            $"<tbody>{rows}</tbody></table></div>";
    }

    private static string DisplayName(string key) =>
        FriendlyNames.TryGetValue(key, out var name)
            ? name
            : key.Replace('.', ' ').Replace('_', ' ').ToUpperInvariant();

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
