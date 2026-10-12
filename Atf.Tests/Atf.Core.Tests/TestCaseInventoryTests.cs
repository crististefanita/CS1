using Atf.Core.Reporting;

namespace Atf.Core.Tests;

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
        public void Fact() { }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void Theory(int value) { }

        [Theory]
        public void TheoryWithoutInlineData() { }

        public void Helper() { }
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
