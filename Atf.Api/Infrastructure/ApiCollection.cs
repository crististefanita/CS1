using Atf.Core.Reporting;
using Xunit;

namespace Atf.Api.Infrastructure;

[CollectionDefinition("API ATF", DisableParallelization = true)]
public sealed class ApiCollection : ICollectionFixture<ExtentCollectionFixture>
{
}
