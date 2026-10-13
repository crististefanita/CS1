namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingTagPolicyTests
{
    [Fact(DisplayName = "report client embeds explicit tag taxonomy and annotation projection")]
    public void ClientEmbedsTagPolicyController()
    {
        var script = ExtentReportClientBehaviour.Build(new ReportingSettings
        {
            TagOrder = ["api", "users"],
            TagTaxonomy = new TagTaxonomySettings
            {
                SelectionTagNames = ["api", "users"],
                DiagnosticTagNames = ["warning", "teardown"]
            }
        });

        Assert.Contains("selection: new Set([\"api\",\"users\"])", script, StringComparison.Ordinal);
        Assert.Contains("diagnostic: new Set([\"teardown\",\"warning\"])", script, StringComparison.Ordinal);
        Assert.Contains("function configureTagSelector()", script, StringComparison.Ordinal);
        Assert.Contains("function removeClassNavigationAnnotations()", script, StringComparison.Ordinal);
        Assert.Contains(".test-detail > .atf-test-annotations", script, StringComparison.Ordinal);
        Assert.Contains("function configureScenarioTagAnnotations()", script, StringComparison.Ordinal);
        Assert.Contains("atf-scenario-annotations", script, StringComparison.Ordinal);
        Assert.DoesNotContain("annotationStrip(tags, 'atf-test-annotations'", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "report client removes non-selectable tags only from the native selector")]
    public void ClientKeepsHierarchyIndependentFromTagSelection()
    {
        var script = ExtentReportClientBehaviour.Build(new ReportingSettings
        {
            TagTaxonomy = new TagTaxonomySettings { SelectionTagNames = ["api"] }
        });

        Assert.Contains("if (tagKind(tag) !== 'selection') item.remove()", script, StringComparison.Ordinal);
        Assert.Contains("scenarioTags(card)", script, StringComparison.Ordinal);
        Assert.Contains("data-atf-tag-kind", script, StringComparison.Ordinal);
        Assert.DoesNotContain("AssignCategory", script, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "report theme styles annotation strips without restoring inherited Extent badges")]
    public void ThemeStylesAnnotationsAsCompactPills()
    {
        var css = AtfReportTheme.BuildCss();

        Assert.Contains(".atf-scenario-annotations", css, StringComparison.Ordinal);
        Assert.Contains(".atf-tag-annotation-selection", css, StringComparison.Ordinal);
        Assert.Contains(".atf-tag-annotation-diagnostic", css, StringComparison.Ordinal);
        Assert.Contains(".test-view .node-attr { display: none !important; }", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "static Extent markup exposes only configured selection tags")]
    public void StaticTagSelectorRemovesDiagnosticItems()
    {
        const string html = """
            <ul id="tag-toggle">
              <a class="dropdown-item" href="#">api</a>
              <a class="dropdown-item" href="#">warning</a>
              <a class="dropdown-item" href="#">users</a>
            </ul>
            """;

        var filtered = ExtentNativeTagSelectorSanitizer.KeepSelectionTags(
            html,
            new AtfTagTaxonomy(["api", "users"], ["warning"]));

        Assert.Contains(">api</a>", filtered, StringComparison.Ordinal);
        Assert.Contains(">users</a>", filtered, StringComparison.Ordinal);
        Assert.DoesNotContain(">warning</a>", filtered, StringComparison.Ordinal);
    }
}
