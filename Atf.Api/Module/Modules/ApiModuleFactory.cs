using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Core.Reporting;
using Atf.Common.Routing;

namespace Atf.Common.Modules;

/// <summary>
/// Explicit composition entry points for the API module. The factory keeps
/// production/settings composition separate from offline port composition;
/// the resulting module remains the same disposable service graph.
/// </summary>
public static class ApiModuleFactory
{
    public static ApiModule FromSettings(
        ApiSettings settings,
        IAtfValidationReporter reporter,
        HttpClient? client = null) =>
        new(settings, reporter, client);

    public static ApiModule FromPorts(
        IApiExecutor executor,
        ApiRoutes routes) =>
        new(executor, routes);
}
