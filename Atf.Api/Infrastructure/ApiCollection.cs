using Atf.Core.Reporting;
using Atf.Api.TestHost;
using Xunit;

namespace Atf.Api.Infrastructure;

[CollectionDefinition("API ATF", DisableParallelization = true)]
public sealed class ApiReportTestGroup : ICollectionFixture<ApiTestFixture>
{
}
