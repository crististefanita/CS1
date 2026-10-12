using Atf.Common.Routing;

namespace Atf.Common.Tests.Architecture;

public sealed class PackageBoundaryTests
{
    [Fact]
    public void CommonNamespacesKeepAtMostEightTopLevelTypes()
    {
        var violations = typeof(ApiRoutes)
            .Assembly
            .GetTypes()
            .Where(type => type.DeclaringType is null)
            .Where(type => type.Namespace?.StartsWith("Atf.Common", StringComparison.Ordinal) == true)
            .GroupBy(type => type.Namespace!)
            .Where(group => group.Count() > 8)
            .Select(group => $"{group.Key}={group.Count()}")
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }
}
