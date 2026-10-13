using Atf.Common.Modules;
using Atf.Common.Routing;
using Atf.Core.Configuration;
using Atf.Core.Configuration.Models;
using Atf.Core.Http;
using Atf.Core.Reporting;
using Xunit;

namespace Atf.Api;

public sealed class ApiModuleFactoryTests
{
    [Fact(DisplayName = "Settings composition creates the complete API service graph")]
    public void FromSettingsCreatesConfiguredGraph()
    {
        var settings = new ApiSettings
        {
            BaseUrl = "https://example.test",
            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["anonymous"] = new AuthProfileSettings { Type = "none" }
            }
        };

        using var module = ApiModuleFactory.FromSettings(settings, new NullReporter());

        Assert.NotNull(module.Executor);
        Assert.NotNull(module.Routes);
        Assert.NotNull(module.Users);
        Assert.NotNull(module.Posts);
    }

    [Fact(DisplayName = "Port composition preserves the supplied executor and routes")]
    public void FromPortsPreservesPorts()
    {
        var executor = new NullExecutor();
        var routes = new ApiRoutes(new RouteSettings());

        using var module = ApiModuleFactory.FromPorts(executor, routes);

        Assert.Same(executor, module.Executor);
        Assert.Same(routes, module.Routes);
    }

    private sealed class NullExecutor : IApiExecutor
    {
        public Task<ApiExecutionResult> ExecuteAsync(ApiRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NullReporter : IAtfReporter
    {
        public void StartStep(string title) { }
        public void CompleteStep(AtfReportStatus status) { }
        public void Detail(string key, string value) { }
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) { }
        public void Info(string message) { }
        public void Pass(string message) { }
        public void Fail(string message, Exception? exception = null) { }
        public void Warning(string message) { }
        public void Skip(string reason) { }
    }
}
