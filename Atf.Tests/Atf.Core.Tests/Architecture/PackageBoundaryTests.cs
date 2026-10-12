using Atf.Core.Configuration;

namespace Atf.Core.Tests.Architecture;

public sealed class PackageBoundaryTests
{
    [Fact]
    public void FrameworkPackagesKeepAtMostEightTopLevelTypesPerNamespace()
    {
        var assemblies = new[]
        {
            typeof(AtfConfiguration).Assembly
        };

        var violations = assemblies
            .SelectMany(assembly => assembly.GetTypes()
                .Where(type => type.DeclaringType is null)
                .Where(type => type.Namespace?.StartsWith("Atf.Core", StringComparison.Ordinal) == true
                    || type.Namespace?.StartsWith("Atf.Common", StringComparison.Ordinal) == true
                    || type.Namespace?.StartsWith("Atf.Common", StringComparison.Ordinal) == true)
                .Select(type => (Assembly: assembly.GetName().Name!, Type: type)))
            .GroupBy(item => (item.Assembly, Namespace: item.Type.Namespace!))
            .Where(group => group.Count() > 8)
            .Select(group => $"{group.Key.Assembly}:{group.Key.Namespace}={group.Count()}")
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }
}
