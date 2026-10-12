using Xunit;

namespace Atf.Api.FailureDemos.Infrastructure;

[CollectionDefinition("API ATF", DisableParallelization = true)]
public sealed class FailureDemoGroup : ICollectionFixture<Atf.Reporting.Extent.ExtentCollectionFixture>
{
}
