using System.Reflection;

namespace Atf.Core.Reporting;

/// <summary>
/// Counts test cases discoverable from a .NET test assembly without coupling
/// the reporting module to xUnit. Theory rows are counted from InlineData;
/// other data providers remain one discoverable method.
/// </summary>
public sealed record AtfTestCaseInventory(int TotalTestCases);

public static class AtfTestCaseInventoryReader
{
    public static AtfTestCaseInventory Read(Assembly? testAssembly)
        => Read(testAssembly?.GetTypes() ?? []);

    /// <summary>
    /// Reads a supplied type set so the discovery contract can be tested without
    /// loading a real runner assembly. The production path uses the assembly
    /// overload; the type-set overload is also useful for custom runners.
    /// </summary>
    public static AtfTestCaseInventory Read(IEnumerable<Type> testTypes)
    {
        ArgumentNullException.ThrowIfNull(testTypes);

        var total = 0;
        foreach (var type in testTypes)
        {
            ArgumentNullException.ThrowIfNull(type);
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attributes = method.GetCustomAttributesData();
                var testAttribute = attributes.FirstOrDefault(attribute =>
                    attribute.AttributeType.Name is "FactAttribute" or "TheoryAttribute");
                if (testAttribute is null)
                {
                    continue;
                }

                if (testAttribute.AttributeType.Name == "TheoryAttribute")
                {
                    var inlineRows = attributes.Count(attribute => attribute.AttributeType.Name == "InlineDataAttribute");
                    total += Math.Max(inlineRows, 1);
                    continue;
                }

                total++;
            }
        }

        return new AtfTestCaseInventory(total);
    }
}
