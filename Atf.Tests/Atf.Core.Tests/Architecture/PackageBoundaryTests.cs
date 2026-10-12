using Atf.Core.Configuration;

namespace Atf.Core.Tests.Architecture;

public sealed class PackageBoundaryTests
{
    [Fact]
    public void Core_namespaces_keep_at_most_ten_top_level_types()
    {
        var violations = typeof(AtfConfiguration)
            .Assembly
            .GetTypes()
            .Where(type => type.DeclaringType is null)
            .Where(type => type.Namespace?.StartsWith("Atf.Core", StringComparison.Ordinal) == true)
            .GroupBy(type => type.Namespace!)
        // Reporting deliberately keeps its public contracts (status, scope,
        // lifecycle and hierarchy) at the package boundary. The threshold
        // leaves room for those OOP seams without allowing unrelated types to
        // accumulate in the root namespace.
        .Where(group => group.Count() > 10)
            .Select(group => $"{group.Key}={group.Count()}")
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }
}
