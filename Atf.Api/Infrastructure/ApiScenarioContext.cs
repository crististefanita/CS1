using Atf.Common.Lifecycle;
using Atf.Common.Steps;
using Atf.Core.Configuration;
using Atf.Core.Reporting;

namespace Atf.Api.Infrastructure;

public sealed record ApiScenarioContext(
    AtfConfiguration Configuration,
    IAtfReporter Reporter,
    UsersSteps Users,
    PostsSteps Posts,
    CleanupRegistry Cleanup);
