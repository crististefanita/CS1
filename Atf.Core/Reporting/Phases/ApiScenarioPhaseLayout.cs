namespace Atf.Core.Reporting.Phases;

/// <summary>
/// Canonical lifecycle vocabulary shared by API execution and report rendering.
/// Keeping these labels in one place prevents a setup/test/teardown scenario
/// from being rendered as an unstructured sequence of sibling steps.
/// </summary>
public static class ApiScenarioPhaseLayout
{
    public const string Setup = "SETUP";
    public const string TestBody = "TEST BODY";
    public const string Teardown = "TEARDOWN";

    public static IReadOnlyList<string> OrderedLabels(bool includeSetup, bool includeTeardown)
    {
        var phases = new List<string>(capacity: 3);
        if (includeSetup)
        {
            phases.Add(Setup);
        }

        phases.Add(TestBody);

        if (includeTeardown)
        {
            phases.Add(Teardown);
        }

        return phases;
    }

    public static string? Classify(string? title)
    {
        if (string.Equals(title?.Trim(), Setup, StringComparison.OrdinalIgnoreCase))
        {
            return "setup";
        }

        if (string.Equals(title?.Trim(), TestBody, StringComparison.OrdinalIgnoreCase))
        {
            return "test-body";
        }

        if (string.Equals(title?.Trim(), Teardown, StringComparison.OrdinalIgnoreCase))
        {
            return "teardown";
        }

        return null;
    }
}
