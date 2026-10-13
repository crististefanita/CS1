using Atf.Api.Infrastructure;
using Xunit;

namespace Atf.Api.Tests.Architecture;

public sealed class InfrastructurePackageBoundaryTests
{
    private const int MaximumTopLevelTypesPerNamespace = 8;

    [Fact(DisplayName = "API infrastructure namespaces stay within the eight-type boundary")]
    public void ApiInfrastructureNamespacesStayWithinBoundary()
    {
        var violations = typeof(ApiTestBase).Assembly
            .GetTypes()
            .Where(type => type.DeclaringType is null)
            .Where(type => type.Namespace?.StartsWith("Atf.Api.Infrastructure", StringComparison.Ordinal) == true)
            .GroupBy(type => type.Namespace!)
            .Where(group => group.Count() > MaximumTopLevelTypesPerNamespace)
            .Select(group => $"{group.Key}={group.Count()}")
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }
}
