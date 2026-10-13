using System.Reflection;
using Atf.Reporting.Extent.Scope;
using Atf.Reporting.Extent.Scope.Ports;
using Atf.Reporting.Extent.Theme.Client;
using Atf.Reporting.Extent.Theme.Client.Composition;
using Atf.Reporting.Extent.Theme.Client.Ports;
using Atf.Reporting.Extent.Theme.Tokens;

namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingPortSeamTests
{
    [Fact(DisplayName = "Scenario facade depends on reporting ports, not concrete policy components")]
    public void ScenarioFacadeUsesPortsForItsThreeIndependentResponsibilities()
    {
        var fields = typeof(ExtentTestScope)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .ToDictionary(field => field.Name, StringComparer.Ordinal);

        Assert.Equal(typeof(IPhaseTreeWriter), fields["_tree"].FieldType);
        Assert.Equal(typeof(IReportEvidencePublisher), fields["_evidence"].FieldType);
        Assert.Equal(typeof(IReportStatusPolicy), fields["_statusPolicy"].FieldType);
        Assert.True(typeof(IPhaseTreeWriter).IsAssignableFrom(typeof(ExtentScopeTreeCoordinator)));
        Assert.True(typeof(IReportEvidencePublisher).IsAssignableFrom(typeof(ExtentScopeEvidenceWriter)));
        Assert.True(typeof(IReportStatusPolicy).IsAssignableFrom(typeof(ExtentScopeStatusPolicy)));
    }

    [Fact(DisplayName = "Theme token provider keeps palette expansion behind a port")]
    public void ThemeFacadeCanUseAnIndependentTokenProvider()
    {
        var tokens = new Dictionary<string, string>(
            new AtfReportThemeTokenProvider().All,
            StringComparer.Ordinal)
        {
            ["__ATF_DARK__"] = "#000000"
        };
        var provider = new StubThemeTokenProvider(tokens);

        var css = AtfReportTheme.BuildCss(provider);

        Assert.Contains("<style id=\"atf-custom-theme\">", css, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_", css, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Client facade delegates to the replaceable projection composer")]
    public void ClientFacadeAndComposerProduceTheSameContract()
    {
        var facade = ExtentReportClientBehaviour.Build();
        var composer = new ExtentReportClientProjectionComposer().Compose();

        Assert.Equal(facade, composer);
        Assert.Contains("function configureNavigationOrder()", facade, StringComparison.Ordinal);
        Assert.DoesNotContain("__ATF_", facade, StringComparison.Ordinal);
    }

    private sealed class StubThemeTokenProvider(IReadOnlyDictionary<string, string> tokens) : IThemeTokenProvider
    {
        public IReadOnlyDictionary<string, string> All { get; } = tokens;
    }
}
