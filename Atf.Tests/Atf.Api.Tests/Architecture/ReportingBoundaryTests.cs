using Atf.Api.Infrastructure;
using Atf.Api.TestHost;
using Xunit;

namespace Atf.Api.Tests.Architecture;

/// <summary>
/// Protects the renderer boundary between live API scenarios and the host
/// composition root. These tests inspect assembly references so a future
/// renderer swap cannot silently re-couple the acceptance assembly to Extent.
/// </summary>
public sealed class ReportingBoundaryTests
{
    private const string ExtentAssemblyName = "Atf.Reporting.Extent";

    [Fact(DisplayName = "API acceptance assembly depends on reporting ports, not concrete Extent")]
    public void ApiAcceptanceAssemblyDoesNotReferenceConcreteExtent()
    {
        var references = typeof(ApiTestBase).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain(ExtentAssemblyName, references);
    }

    [Fact(DisplayName = "composition host is the live boundary that selects Extent")]
    public void CompositionHostReferencesConcreteExtent()
    {
        var references = typeof(ApiTestFixture).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(ExtentAssemblyName, references);
    }

    [Fact(DisplayName = "scenario base exposes only the neutral report-session port")]
    public void ScenarioBaseConstructorUsesNeutralSessionPort()
    {
        var constructor = typeof(ApiTestBase)
            .GetConstructors(System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic)
            .Single();
        var parameterTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Contains(typeof(IAtfApiReportSession), parameterTypes);
        Assert.DoesNotContain(parameterTypes, type => type.FullName?.StartsWith("Atf.Reporting.Extent", StringComparison.Ordinal) == true);
    }
}
