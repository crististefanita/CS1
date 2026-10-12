using Atf.Core.Reporting;

namespace Atf.Core.Tests.Reporting;

public sealed class TestCaseInventoryTests
{
    [Xunit.Fact(DisplayName = "Test case inventory treats a null assembly as an empty suite")]
    public void NullAssemblyProducesEmptyInventory()
    {
        var inventory = AtfTestCaseInventoryReader.Read((System.Reflection.Assembly?)null);

        Assert.Equal(0, inventory.TotalTestCases);
    }

    [Xunit.Fact(DisplayName = "Test case inventory counts facts and inline theory rows")]
    public void InventoryCountsDiscoverableRows()
    {
        var inventory = AtfTestCaseInventoryReader.Read([typeof(SyntheticCases)]);

        Assert.Equal(4, inventory.TotalTestCases);
    }

    private sealed class SyntheticCases
    {
        [Fact]
        public static void FactCase() { }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public static void TheoryCase(int value) { }

        [Theory]
        public static void TheoryWithoutInlineDataCase() { }

        public static void HelperCase() { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class FactAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class TheoryAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    private sealed class InlineDataAttribute : Attribute
    {
        public InlineDataAttribute(params object[] _) { }
    }
}
