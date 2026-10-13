namespace Atf.Core.Tests.Architecture;

public sealed class TagTaxonomyTests
{
    private static AtfTagTaxonomy Policy() => new(
        ["api", "users", "contract"],
        ["warning", "failure", "teardown"]);

    [Fact(DisplayName = "tag taxonomy treats only explicitly configured tags as selectable")]
    public void SelectionEligibilityIsExplicit()
    {
        var policy = Policy();

        Assert.Equal(AtfTagKind.Selection, policy.Classify(" api "));
        Assert.Equal(AtfTagKind.Diagnostic, policy.Classify("warning"));
        Assert.Equal(AtfTagKind.Diagnostic, policy.Classify("future-operational-marker"));
    }

    [Fact(DisplayName = "tag taxonomy normalizes and deduplicates scenario metadata")]
    public void NormalizationIsStableAndCaseInsensitive()
    {
        var tags = AtfTagTaxonomy.NormalizeAndDistinct([" api ", "API", "", " users ", "users"]);

        Assert.Equal(["api", "users"], tags);
    }

    [Fact(DisplayName = "tag taxonomy separates selection tags from diagnostic metadata")]
    public void PolicyProjectsIndependentSurfaces()
    {
        var policy = Policy();
        string[] source = ["api", "warning", "users", "failure", "unknown"];

        Assert.Equal(["api", "users"], policy.SelectableTags(source));
        Assert.Equal(["warning", "failure", "unknown"], policy.DiagnosticTags(source));
    }

    [Fact(DisplayName = "diagnostic tags win when a configuration typo lists both kinds")]
    public void DiagnosticClassificationHasPrecedence()
    {
        var policy = new AtfTagTaxonomy(["api", "warning"], ["warning"]);

        Assert.Equal(AtfTagKind.Diagnostic, policy.Classify("warning"));
        Assert.DoesNotContain("warning", policy.SelectableTags(["warning"]));
    }
}
