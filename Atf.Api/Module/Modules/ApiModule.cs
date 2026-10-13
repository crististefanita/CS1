using Atf.Common.Routing;
using Atf.Common.Services;
using Atf.Core.Configuration;
using Atf.Core.Http;
using Atf.Core.Reporting;

namespace Atf.Common.Modules;

/// <summary>
/// Composition root for the API module. Tests consume domain steps and do not
/// need to know how HttpClient, authentication, routing and reporting are wired.
/// </summary>
public sealed class ApiModule : IDisposable
{
    private readonly HttpClient? _ownedClient;

    public ApiModule(ApiSettings settings, IAtfValidationReporter reporter, HttpClient? client = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(reporter);

        _ownedClient = client is null ? new HttpClient() : null;
        var httpClient = client ?? _ownedClient!;
        Routes = new ApiRoutes(settings.Routes);
        Executor = new ApiExecutor(httpClient, settings, reporter);
        Users = new UsersService(Executor, Routes);
        Posts = new PostsService(Executor, Routes);
    }

    public ApiModule(IApiExecutor executor, ApiRoutes routes)
    {
        Executor = executor ?? throw new ArgumentNullException(nameof(executor));
        Routes = routes ?? throw new ArgumentNullException(nameof(routes));
        Users = new UsersService(Executor, Routes);
        Posts = new PostsService(Executor, Routes);
    }

    public IApiExecutor Executor { get; }
    public ApiRoutes Routes { get; }
    public UsersService Users { get; }
    public PostsService Posts { get; }

    public void Dispose()
    {
        _ownedClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
