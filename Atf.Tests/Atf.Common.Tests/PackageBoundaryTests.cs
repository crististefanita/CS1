using Atf.Common.Routing;

namespace Atf.Common.Tests.Architecture;

public sealed class PackageBoundaryTests
{
    private const int MaximumTopLevelTypesPerNamespace = 8;

    [Fact]
    public void ApiAndCommonNamespacesDoNotGrowBeyondTheEightTypeBoundary()
    {
        var violations = typeof(ApiRoutes)
            .Assembly
            .GetTypes()
            .Where(type => type.DeclaringType is null)
            .Where(type => type.Namespace?.StartsWith("Atf.Api", StringComparison.Ordinal) == true
                || type.Namespace?.StartsWith("Atf.Common", StringComparison.Ordinal) == true)
            .GroupBy(type => type.Namespace!)
            .Where(group => group.Count() > MaximumTopLevelTypesPerNamespace)
            .Select(group => $"{group.Key}={group.Count()}")
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }
}
