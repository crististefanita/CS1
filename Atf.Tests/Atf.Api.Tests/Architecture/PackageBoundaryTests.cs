using Atf.Api.TestHost;
using Xunit;

namespace Atf.Api.Tests.Architecture;

public sealed class PackageBoundaryTests
{
    private const int MaximumTopLevelTypesPerNamespace = 8;

    [Fact]
    public void TestHostNamespacesStayWithinTheEightTypeBoundary()
    {
        var violations = typeof(ApiTestFixture).Assembly
            .GetTypes()
            .Where(type => type.DeclaringType is null)
            .Where(type => type.Namespace?.StartsWith("Atf.Api.TestHost", StringComparison.Ordinal) == true)
            .GroupBy(type => type.Namespace!)
            .Where(group => group.Count() > MaximumTopLevelTypesPerNamespace)
            .Select(group => $"{group.Key}={group.Count()}")
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }
}
