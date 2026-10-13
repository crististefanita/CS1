using Atf.Common.Lifecycle;
using Atf.Common.Services;
using Atf.Core.Configuration;
using Atf.Core.Reporting;

namespace Atf.Api.Infrastructure;

public sealed record ApiScenarioContext(
    AtfConfiguration Configuration,
    IAtfValidationReporter Reporter,
    UsersService Users,
    PostsService Posts,
    CleanupRegistry Cleanup);
