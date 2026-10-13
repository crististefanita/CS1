namespace Atf.Reporting.Extent.Scope;

/// <summary>
/// Centralizes the small amount of semantic classification needed by the
/// Extent tree. Keeping it outside the scope object prevents presentation
/// titles from becoming scattered lifecycle rules.
/// </summary>
internal static class ExtentStepClassifier
{
    private static readonly HashSet<string> HttpVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE"
    };

    public static bool IsPhase(string title) => ApiScenarioPhaseLayout.Classify(title) is not null;

    public static bool IsHttp(string title)
    {
        var verb = title.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return verb is not null && HttpVerbs.Contains(verb);
    }

    public static bool IsValidation(string title) =>
        title.StartsWith("Validate:", StringComparison.OrdinalIgnoreCase);
}
