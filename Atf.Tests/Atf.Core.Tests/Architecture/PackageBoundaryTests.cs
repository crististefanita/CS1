using Atf.Core.Configuration;

namespace Atf.Core.Tests.Architecture;

public sealed class PackageBoundaryTests
{
    private const int MaximumTopLevelTypesPerNamespace = 8;

    [Fact]
    public void CorePackagesDoNotGrowBeyondTheEightTypeBoundary()
    {
        var violations = typeof(AtfConfiguration).Assembly
            .GetTypes()
            .Where(type => type.DeclaringType is null)
            .Where(type => type.Namespace?.StartsWith("Atf.Core", StringComparison.Ordinal) == true)
            .GroupBy(type => type.Namespace!)
            .Where(group => group.Count() > MaximumTopLevelTypesPerNamespace)
            .Select(group => $"{group.Key}={group.Count()}")
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }
}
