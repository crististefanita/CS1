using Atf.Reporting.Extent;

namespace Atf.Reporting.Extent.Tests;

public sealed class PackageBoundaryTests
{
    private const int MaximumTopLevelTypesPerNamespace = 8;

    [Fact]
    public void ReportingNamespacesDoNotGrowBeyondTheEightTypeBoundary()
    {
        var violations = typeof(ExtentReportLifecycle).Assembly
            .GetTypes()
            .Where(type => type.DeclaringType is null)
            .Where(type => type.Namespace?.StartsWith("Atf.Reporting.Extent", StringComparison.Ordinal) == true)
            .GroupBy(type => type.Namespace!)
            .Where(group => group.Count() > MaximumTopLevelTypesPerNamespace)
            .Select(group => $"{group.Key}={group.Count()}")
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }
}
