using Atf.Core.Configuration;

namespace Atf.Core.Tests.Architecture;

public sealed class PackageBoundaryTests
{
    [Fact]
    public void Core_namespaces_keep_at_most_eight_top_level_types()
    {
        var violations = typeof(AtfConfiguration)
            .Assembly
            .GetTypes()
            .Where(type => type.DeclaringType is null)
            .Where(type => type.Namespace?.StartsWith("Atf.Core", StringComparison.Ordinal) == true)
            .GroupBy(type => type.Namespace!)
            .Where(group => group.Count() > 8)
            .Select(group => $"{group.Key}={group.Count()}")
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }
}
